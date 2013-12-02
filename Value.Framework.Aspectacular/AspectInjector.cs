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
    public class Interceptor
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

        public Interceptor(Func<object> instanceFactory, Action<object> instanceCleaner, params Aspect[] aspects)
        {
            this.instanceResolverFunc = instanceFactory;
            this.instanceCleanerFunc = instanceCleaner;

            foreach (Aspect aspect in aspects)
            {
                aspect.Context = this;
                this.aspects.AddRange(aspects);
            }
        }

        public Interceptor(Func<object> instanceFactory, params Aspect[] aspects)
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
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TOut>> callExpression)
        {
            Func<TOut> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

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
        /// <param name="callExpression"></param>
        public void Invoke(Expression<Action> callExpression)
        {
            Action blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            this.ExecuteMainSequence(() => blDelegate.Invoke());
        }
    }

    public class InstanceInterceptor<TInstance> : Interceptor
        where TInstance : class
    {
        public new TInstance AugmentedClassInstance
        {
            get { return (TInstance)base.AugmentedClassInstance; }
        }

        public InstanceInterceptor(Func<TInstance> instanceFactory, Action<TInstance> instanceCleaner, Aspect[] aspects)
            : base(instanceFactory, 
                   inst => 
                    { 
                        if (instanceCleaner != null ) 
                            instanceCleaner((TInstance)inst); 
                    }, 
                   aspects)
        {
        }

        public InstanceInterceptor(Func<TInstance> instanceFactory, params Aspect[] aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }

        public InstanceInterceptor(TInstance instance, params Aspect[] aspects)
            : this(() => instance, instanceCleaner: null, aspects: aspects)
        {
        }

        /// <summary>
        /// Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TInstance, TOut>> callExpression)
        {
            this.ResolveClassInstance();

            Func<TInstance, TOut> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke(this.AugmentedClassInstance);
                this.MethodExecutionResult = retVal;
            });
            
            return retVal;
        }


        /// <summary>
        /// Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <param name="callExpression"></param>
        public void Invoke(Expression<Action<TInstance>> callExpression)
        {
            this.ResolveClassInstance();

            Action<TInstance> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            this.ExecuteMainSequence(() => blDelegate.Invoke(this.AugmentedClassInstance));
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
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(Aspect[] aspects, Expression<Func<TOut>> callExpression)
        {
            var context = new Interceptor(null, aspects);
            TOut retVal = context.Invoke<TOut>(callExpression);
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return result.
        /// </summary>
        /// <param name="aspects"></param>
        /// <param name="callExpression"></param>
        public static void Invoke(Aspect[] aspects, Expression<Action> callExpression)
        {
            var context = new Interceptor(null, aspects);
            context.Invoke(callExpression);
        }

        public static InstanceInterceptor<TInstance> GetProxy<TInstance>(this TInstance instance, params Aspect[] aspects)
            where TInstance : class
        {
            var interceptor = new InstanceInterceptor<TInstance>(instance, aspects);
            return interceptor;
        }
    }
}
