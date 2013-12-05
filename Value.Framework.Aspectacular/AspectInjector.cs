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
    /// <summary>
    /// Main base class encapsulating call interception and aspect injection logic.
    /// </summary>
    public class Interceptor
    {
        /// <summary>
        /// Instance of an object whose methods are intercepted.
        /// Null when static methods are intercepted.
        /// Can be an derived from IAspect of object wants to be its own 
        /// </summary>
        protected object AugmentedClassInstance { get; set; }
        protected Delegate interceptedMethod;
        protected readonly List<IAspect> aspects = new List<IAspect>();

        private Func<object> instanceResolverFunc;
        private Action<object> instanceCleanerFunc;
        private volatile bool isUsed = false;
        private Action interceptedMethodCaller;

        internal bool methodCalled = false;

        public object MethodExecutionResult { get; internal set; }
        public Exception MethodExecutionException { get; protected set; }
        public InterceptedMethodMetadata InterceptedCallMetaData { get; protected set; }
        public bool CancelInterceptedMethodCall { get; internal set; }

        /// <summary>
        /// Aspects may set this to true to break break aspect call sequence
        /// </summary>
        public bool StopAspectCallChain { get; set; }

        /// <summary>
        /// Returns true if an attempt of executing intercepted method was made 
        /// and it ended with an exception thrown by method, by return result post-processor, 
        /// or aspects running right after intercepted method call.
        /// </summary>
        public bool InterceptedMedthodCallFailed { get { return this.MethodExecutionException != null; } }

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

            // Augmented object can be interception context aware.
            if (this.AugmentedClassInstance is IInterceptionContext)
                ((IInterceptionContext)this.AugmentedClassInstance).Context = this;

            // Augmented object can be aspect for its own method interceptions.
            if (this.AugmentedClassInstance is IAspect)
                this.aspects.Add(this.AugmentedClassInstance as IAspect);
        }

        /// <summary>
        /// Method call wrapper that calls aspects and the intercepted method.
        /// </summary>
        /// <param name="interceptedMethodCallerClosure">Intercepted method call wrapped in an interceptor's closure.</param>
        protected virtual void ExecuteMainSequence(Action interceptedMethodCallerClosure)
        {
            if (this.isUsed)
                throw new Exception("Same instance of the call interceptor cannot be used more than once.");

            this.isUsed = true;

            this.MethodExecutionResult = null;
            this.MethodExecutionException = null;
            this.methodCalled = false;

            this.interceptedMethodCaller = interceptedMethodCallerClosure;

            try
            {
                this.CallAspects(aspect => aspect.Step_2_BeforeTryingMethodExec());

                try
                {
                    if (!this.CancelInterceptedMethodCall)
                    {
                        this.methodCalled = true;
                        this.InvokeInterceptedMethod();
                    }
                }
                catch (Exception ex)
                {
                    this.MethodExecutionException = ex;
                    this.CallAspects(aspect => aspect.Step_4_Optional_AfterCatchingMethodExecException());
                    throw;
                }
                finally
                {
                    this.CallAspectsBackwards(aspect => aspect.Step_5_FinallyAfterMethodExecution(!this.InterceptedMedthodCallFailed));
                }
            }
            finally
            {
                if (this.instanceCleanerFunc != null)
                {
                    try
                    {
                        this.instanceCleanerFunc.Invoke(this.AugmentedClassInstance);

                        this.CallAspectsBackwards(aspect => aspect.Step_6_Optional_AfterInstanceCleanup());
                    }
                    finally
                    {
                        if (this.AugmentedClassInstance is IInterceptionContext)
                            (this.AugmentedClassInstance as IInterceptionContext).Context = null;
                    }
                }
            }
        }

        /// <summary>
        /// Last stop before calling (possibly wrapped) intercepted method.
        /// </summary>
        protected virtual void InvokeInterceptedMethod()
        {
            // Non-void return calls have another Aspect sequence call between after getting return and massaging it.
            this.interceptedMethodCaller.Invoke(); 
        }

        private void CallAspects(Action<Aspect> cutPointHandler)
        {
            if (cutPointHandler == null)
                return;

            this.StopAspectCallChain = false;

            foreach (Aspect aspect in this.aspects)
            {
                cutPointHandler.Invoke(aspect);

                if (this.StopAspectCallChain)
                    break;
            }
        }

        private void CallAspectsBackwards(Action<Aspect> cutPointHandler)
        {
            if (cutPointHandler == null)
                return;

            this.StopAspectCallChain = false;

            foreach (Aspect aspect in this.aspects.ReverseOrder())
            {
                cutPointHandler(aspect);

                if (this.StopAspectCallChain)
                    break;
            }
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
        public TOut Invoke<TOut>(Expression<Func<TOut>> callExpression, Func<TOut, object> retValPostProcessor = null)
        {
            Func<TOut> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke();

                this.CallReturnValuePostProcessor<TOut>(retValPostProcessor, retVal);
            });

            return retVal;
        }

        protected void CallReturnValuePostProcessor<TOut>(Func<TOut, object> retValPostProcessor, TOut retVal)
        {
            this.MethodExecutionResult = retVal;

            this.CallAspects(aspect => aspect.Step_3_BeforeMassagingReturnedResult());

            if (retValPostProcessor != null && this.MethodExecutionResult != null)
                this.MethodExecutionResult = retValPostProcessor(retVal);
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
}
