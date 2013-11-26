using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public abstract class Aspect
    {
        public DalContextBase Context { get; set; }

        public Aspect() { }

        public virtual void Step_1_BeforeResolvingInstance() { }

        public virtual void Step_2_BeforeTryingMethodExec() { }
        public virtual void Step_3_Optional_AfterCatchingMethodExecException() { }
        public virtual void Step_4_FinallyAfterMethodExecution() { }

        public virtual void Step_5_Optional_AfterInstanceCleanup() { }
    }

    public abstract class DalContextBase
    {
        public object AugmentedClassInstance { get; protected set; }
        //protected IDisposable AugmentedDisposableInstance { get { return this.AugmentedClassInstance as IDisposable; } }

        protected LambdaExpression methodExp { get; set; }
        protected Delegate blMethod;

        //private bool responsibleForDeallocation;
        private Func<object> instanceResolverFunc;
        private Action<object> instanceCleanerFunc;

        public object MethodExecutionResult { get; protected set; }
        public Exception MethodExecutionException { get; protected set; }

        public bool MedthodHasFailed { get { return this.MethodExecutionException != null; } }

        protected readonly List<Aspect> aspects = new List<Aspect>();

        public DalContextBase(Func<object> instanceFactory, Action<object> instanceCleaner, params Aspect[] aspects)
        {
            this.instanceResolverFunc = instanceFactory;
            this.instanceCleanerFunc = instanceCleaner;

            foreach (Aspect aspect in aspects)
            {
                aspect.Context = this;
                this.aspects.AddRange(aspects);
            }
        }

        public DalContextBase(Func<object> instanceFactory, params Aspect[] aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }

        protected virtual void ResolveClassInstance()
        {
            this.CallAspects(aspect => aspect.Step_1_BeforeResolvingInstance());
            this.AugmentedClassInstance = this.instanceResolverFunc();
        }

        protected virtual void ExecuteMainSequence(Action blMethodCaller)
        {
            if (this.AugmentedClassInstance == null)
                throw new Exception("Instance for AOP augmentation needs to be specified before intercepted method can be called.");

            this.CallAspects(aspect => aspect.Step_2_BeforeTryingMethodExec());

            this.MethodExecutionResult = null;
            this.MethodExecutionException = null;

            try
            {
                blMethodCaller.Invoke();
            }
            catch (Exception ex)
            {
                this.MethodExecutionException = ex;
                this.CallAspects(aspect => aspect.Step_3_Optional_AfterCatchingMethodExecException());
            }
            finally
            {
                this.CallAspectsBackwards(aspect => aspect.Step_4_FinallyAfterMethodExecution());
            }

            if (this.instanceCleanerFunc != null)
            {
                try
                {
                    this.instanceCleanerFunc.Invoke(this.AugmentedClassInstance);
                }
                finally
                {
                    this.CallAspectsBackwards(aspect => aspect.Step_5_Optional_AfterInstanceCleanup());
                }
            }
        }

        private void CallAspects(Action<Aspect> cutPointHandler)
        {
            if (cutPointHandler == null)
                return;

            foreach (Aspect aspect in this.aspects)
                cutPointHandler.Invoke(aspect);
        }

        private void CallAspectsBackwards(Action<Aspect> cutPointHandler)
        {
            if (cutPointHandler == null)
                return;

            foreach (Aspect aspect in this.aspects.ReverseOrder())
                cutPointHandler(aspect);
        }

        protected void InitMethodMetadata(MethodCallExpression methodExp)
        {
            // TBD
            methodExp.Method.ToString();
        }
    }

    public class DalContext<TInstance> : DalContextBase
        where TInstance : class
    {
        public new TInstance AugmentedClassInstance
        {
            get { return (TInstance)base.AugmentedClassInstance; }
        }

        public DalContext(Func<TInstance> instanceFactory, Action<TInstance> instanceCleaner, params Aspect[] aspects)
            : base(instanceFactory, 
                   inst => 
                    { 
                        if (instanceCleaner != null ) 
                            instanceCleaner((TInstance)inst); 
                    }, 
                   aspects)
        {
        }

        public DalContext(Func<TInstance> instanceFactory, params Aspect[] aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }


        public TOut Execute<TOut>(Func<TInstance, Expression<Func<TOut>>> proxy)
        {
            this.ResolveClassInstance();

            Expression<Func<TOut>> blClosureExp = proxy(this.AugmentedClassInstance as TInstance);
            this.InitMethodMetadata((MethodCallExpression)blClosureExp.Body);

            this.methodExp = blClosureExp;

            Func<TOut> blDelegate = blClosureExp.Compile();
            this.blMethod = blDelegate;

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke();
                this.MethodExecutionResult = retVal;
            });
            return retVal;
        }
    }

    public static partial class AOP
    {
        public static TOut RunAugmented<TInstance, TOut>(this TInstance instance, Func<TInstance, Expression<Func<TOut>>> proxy, params Aspect[] aspects)
            where TInstance : class
        {
            var context = new DalContext<TInstance>(() => instance, aspects);
            TOut retVal = context.Execute<TOut>(proxy);
            return retVal;
        }
    }
}
