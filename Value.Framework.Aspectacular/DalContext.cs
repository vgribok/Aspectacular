using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public class InterceptedMethodMetadata
    {
        protected MethodCallExpression interceptedMethodExpression;
        public MethodInfo MethodReflectionInfo { get; private set; }
        public IEnumerable<Attribute> MethodAttributes { get { return this.MethodReflectionInfo.GetCustomAttributes(); } }

        public InterceptedMethodMetadata(LambdaExpression callLambdaExp)
        {
            try
            {
                this.interceptedMethodExpression = (MethodCallExpression)callLambdaExp.Body;
            }
            catch (Exception ex)
            {
                string errorText = "Intercepted method expression must be a function call. \"{0}\" is invalid in this context."
                                        .SmartFormat(callLambdaExp.Body);
                throw new ArgumentException(errorText, ex);
            }

            this.MethodReflectionInfo = this.interceptedMethodExpression.Method;
        }
    }

    public class DalContextBase
    {
        public object AugmentedClassInstance { get; protected set; }
        //protected IDisposable AugmentedDisposableInstance { get { return this.AugmentedClassInstance as IDisposable; } }

        protected Delegate interceptedMethod;

        private Func<object> instanceResolverFunc;
        private Action<object> instanceCleanerFunc;

        public object MethodExecutionResult { get; protected set; }
        public Exception MethodExecutionException { get; protected set; }
        public InterceptedMethodMetadata InterceptedCallMetaData { get; protected set; }

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

            if (this.AugmentedClassInstance == null)
                throw new Exception("Instance for AOP augmentation needs to be specified before intercepted method can be called.");
        }

        protected virtual void ExecuteMainSequence(Action interceptedMethodCaller)
        {
            this.CallAspects(aspect => aspect.Step_2_BeforeTryingMethodExec());

            this.MethodExecutionResult = null;
            this.MethodExecutionException = null;

            try
            {
                interceptedMethodCaller.Invoke();
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

        protected void InitMethodMetadata(LambdaExpression callLambdaWrapper, Delegate interceptedMethod)
        {
            this.interceptedMethod = interceptedMethod;
            this.InterceptedCallMetaData = new InterceptedMethodMetadata(callLambdaWrapper);
        }

        /// <summary>
        /// Executes/intercepts *static* function with TOut return result.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public TOut Execute<TOut>(Func<Expression<Func<TOut>>> proxy)
        {
            // Call proxy to return method call (lambda) expression
            Expression<Func<TOut>> blClosureExp = proxy.Invoke();

            Func<TOut> blDelegate = blClosureExp.Compile();
            this.InitMethodMetadata(blClosureExp, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke();
                this.MethodExecutionResult = retVal;
            });
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return value.
        /// </summary>
        /// <param name="proxy"></param>
        public void Execute(Func<Expression<Action>> proxy)
        {
            // Call proxy to return method call (lambda) expression
            Expression<Action> blClosureExp = proxy.Invoke();

            Action blDelegate = blClosureExp.Compile();
            this.InitMethodMetadata(blClosureExp, blDelegate);

            this.ExecuteMainSequence(() => blDelegate.Invoke());
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

        /// <summary>
        /// Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public TOut Execute<TOut>(Func<TInstance, Expression<Func<TOut>>> proxy)
        {
            this.ResolveClassInstance();

            // Call proxy to return method call (lambda) expression
            Expression<Func<TOut>> blClosureExp = proxy(this.AugmentedClassInstance as TInstance);

            Func<TOut> blDelegate = blClosureExp.Compile();
            this.InitMethodMetadata(blClosureExp, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke();
                this.MethodExecutionResult = retVal;
            });
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <param name="proxy"></param>
        public void Execute(Func<TInstance, Expression<Action>> proxy)
        {
            this.ResolveClassInstance();

            // Call proxy to return method call (lambda) expression
            Expression<Action> blClosureExp = proxy(this.AugmentedClassInstance as TInstance);

            Action blDelegate = blClosureExp.Compile();
            this.InitMethodMetadata(blClosureExp, blDelegate);

            this.ExecuteMainSequence(() => blDelegate.Invoke());
        }
    }

    /// <summary>
    /// Extensions and static convenience methods for intercepted method calls.
    /// </summary>
    public static partial class AOP
    {
        /// <summary>
        /// Executes/intercepts *static* function with TOut return result.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static TOut RunAugmented<TOut>(Aspect[] aspects, Func<Expression<Func<TOut>>> proxy)
        {
            var context = new DalContextBase(null, aspects);
            TOut retVal = context.Execute<TOut>(proxy);
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return result.
        /// </summary>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        public static void RunAugmented(Aspect[] aspects, Func<Expression<Action>> proxy)
        {
            var context = new DalContextBase(null, aspects);
            context.Execute(proxy);
        }

        /// <summary>
        /// Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static TOut RunAugmented<TInstance, TOut>(this TInstance instance, Aspect[] aspects, Func<TInstance, Expression<Func<TOut>>> proxy)
            where TInstance : class
        {
            var context = new DalContext<TInstance>(() => instance, aspects);
            TOut retVal = context.Execute<TOut>(proxy);
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        public static void RunAugmented<TInstance>(this TInstance instance, Aspect[] aspects, Func<TInstance, Expression<Action>> proxy)
            where TInstance : class
        {
            var context = new DalContext<TInstance>(() => instance, aspects);
            context.Execute(proxy);
        }
    }
}
