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
    public class Proxy
    {
        #region Limited fields and properties

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

        public bool MethodWasCalled { get; protected set; }

        #endregion Limited fields and properties

        #region Public fields and properties

        public object MethodReturnedValue { get; internal set; }
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

        /// <summary>
        /// Determines returned data cache-ability.
        /// Returns true if this method will return same data if called at the same time
        /// on two or more class instances (or on the same type for static methods).
        /// </summary>
        /// <remarks>
        /// Aspects implementing caching must examine this property before caching data.
        /// Mark classes and methods with InvariantReturnAttribute to mark them as cacheable or not.
        /// </remarks>
        public bool CanCacheReturnedResult
        {
            get { return this.InterceptedCallMetaData.IsReturnResultInvariant; }
        }

        #endregion Public fields and properties

        #region Constructors

        public Proxy(Func<object> instanceFactory, Action<object> instanceCleaner, params Aspect[] aspects)
        {
            this.instanceResolverFunc = instanceFactory;
            this.instanceCleanerFunc = instanceCleaner;

            foreach (Aspect aspect in aspects)
            {
                aspect.Context = this;
                this.aspects.AddRange(aspects);
            }
        }

        public Proxy(Func<object> instanceFactory, params Aspect[] aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }

        #endregion Constructors

        #region Steps in sequence

        protected virtual void Step_1_BeforeResolvingInstance()
        {
            this.CallAspects(aspect => aspect.Step_1_BeforeResolvingInstance());
        }

        /// <summary>
        /// Not called for intercepted static methods
        /// </summary>
        protected virtual void ResolveClassInstance()
        {
            this.Step_1_BeforeResolvingInstance();
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

        protected virtual void Step_2_BeforeTryingMethodExec()
        {
            this.CallAspects(aspect => aspect.Step_2_BeforeTryingMethodExec());
        }

        protected virtual void InvokeActualInterceptedMethod(Action interceptedMethodClosure)
        {
            interceptedMethodClosure.Invoke();
        }

        protected virtual void Step_3_BeforeMassagingReturnedResult()
        {
            this.CallAspects(aspect => aspect.Step_3_BeforeMassagingReturnedResult());
        }

        protected virtual void Step_4_Optional_AfterCatchingMethodExecException()
        {
            this.CallAspects(aspect => aspect.Step_4_Optional_AfterCatchingMethodExecException());
        }

        protected virtual void Step_5_FinallyAfterMethodExecution()
        {
            this.CallAspectsBackwards(aspect => aspect.Step_5_FinallyAfterMethodExecution(!this.InterceptedMedthodCallFailed));
        }

        protected virtual void Step_6_Optional_AfterInstanceCleanup()
        {
            this.CallAspectsBackwards(aspect => aspect.Step_6_Optional_AfterInstanceCleanup());
        }

        #endregion Steps in sequence

        /// <summary>
        /// Method call wrapper that calls aspects and the intercepted method.
        /// </summary>
        /// <param name="interceptedMethodCallerClosure">Intercepted method call wrapped in an interceptor's closure.</param>
        protected void ExecuteMainSequence(Action interceptedMethodCallerClosure)
        {
            if (this.isUsed)
                throw new Exception("Same instance of the call interceptor cannot be used more than once.");

            this.isUsed = true;

            this.MethodReturnedValue = null;
            this.MethodExecutionException = null;
            this.MethodWasCalled = false;

            try
            {
                this.Step_2_BeforeTryingMethodExec();

                try
                {
                    if (!this.CancelInterceptedMethodCall)
                    {
                        this.MethodWasCalled = true;
                        interceptedMethodCallerClosure.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    this.MethodExecutionException = ex;
                    this.Step_4_Optional_AfterCatchingMethodExecException();
                    throw;
                }
                finally
                {
                    this.Step_5_FinallyAfterMethodExecution();
                }
            }
            finally
            {
                if (this.instanceCleanerFunc != null)
                {
                    try
                    {
                        this.instanceCleanerFunc.Invoke(this.AugmentedClassInstance);
                        this.Step_6_Optional_AfterInstanceCleanup();
                    }
                    finally
                    {
                        if (this.AugmentedClassInstance is IInterceptionContext)
                            (this.AugmentedClassInstance as IInterceptionContext).Context = null;
                    }
                }
            }
        }

        #region Utility methods

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
            this.InterceptedCallMetaData = new InterceptedMethodMetadata(this.AugmentedClassInstance, callLambdaWrapper);
        }

        protected void CallReturnValuePostProcessor<TOut>(Func<TOut, object> retValPostProcessor, TOut retVal)
        {
            this.MethodReturnedValue = retVal;

            this.Step_3_BeforeMassagingReturnedResult();

            if (retValPostProcessor != null && this.MethodReturnedValue != null)
                this.MethodReturnedValue = retValPostProcessor(retVal);
        }

        #endregion Utility methods

        /// <summary>
        /// Executes/intercepts *static* function with TOut return result.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="interceptedCallExpression"></param>
        /// <param name="retValPostProcessor">
        /// Delegate called immediately after callExpression function was executed. 
        /// Allows additional massaging of the returned value. Useful when LINQ suffix functions, like ToList(), Single(), etc. 
        /// need to be called in alloc/invoke/dispose pattern.
        /// </param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TOut>> interceptedCallExpression, Func<TOut, object> retValPostProcessor = null)
        {
            Func<TOut> blDelegate = interceptedCallExpression.Compile();
            this.InitMethodMetadata(interceptedCallExpression, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                this.InvokeActualInterceptedMethod(() => retVal = blDelegate.Invoke());
                this.CallReturnValuePostProcessor<TOut>(retValPostProcessor, retVal);
            });

            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return value.
        /// </summary>
        /// <param name="interceptedCallExpression"></param>
        public void Invoke(Expression<Action> interceptedCallExpression)
        {
            Action blDelegate = interceptedCallExpression.Compile();
            this.InitMethodMetadata(interceptedCallExpression, blDelegate);

            this.ExecuteMainSequence(() => this.InvokeActualInterceptedMethod(() => blDelegate.Invoke()));
        }

        /// <summary>
        /// Returns cache key and function returned value.
        /// It's slow as parameters get evaluated via Expression.Compile() and reflection Invoke().
        /// </summary>
        /// <param name="cacheKey">Key that uniquely identifies method and its parameter values.</param>
        /// <returns></returns>
        public object SlowGetReturnValueForCaching(out string cacheKey)
        {
            this.RequirePostExecutionPhase();

            if(!this.CanCacheReturnedResult)
                throw new Exception(string.Format("This method/class is not marked with [InvariantReturnAttribute]: \"{0}\".", this.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.NoValue)));

            cacheKey = this.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);

            object retVal = this.GetReturnValueInternal(makeSecret: false);
            return retVal;
        }

        /// <summary>
        /// Returns string representation of method's return value;
        /// </summary>
        /// <param name="trueUI_falseInternal"></param>
        /// <returns></returns>
        public string FormateReturnValue(bool trueUI_falseInternal)
        {
            this.RequirePostExecutionPhase();

            string retValStr = InterceptedMethodParamMetadata.FormatParamValue(
                                    this.InterceptedCallMetaData.MethodReturnType, 
                                    this.GetReturnValueInternal(this.InterceptedCallMetaData.IsReturnValueSecret), 
                                    trueUI_falseInternal);
            return retValStr;
        }

        #region Utility methods

        /// <summary>
        /// Returns exception object for failed calls,
        /// string.Empty for void return types, 
        /// and actual returned result for successful non-void calls.
        /// </summary>
        /// <returns></returns>
        private object GetReturnValueInternal(bool makeSecret)
        {
            this.RequirePostExecutionPhase();

            object retVal;

            if (this.InterceptedMedthodCallFailed)
                retVal = this.MethodExecutionException;
            else
            {
                if (this.InterceptedCallMetaData.IsStaticMethod)
                    retVal = string.Empty;
                else
                {
                    retVal = this.MethodReturnedValue;
                    if (makeSecret)
                        retVal = new SecretValueHash(retVal);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Ensures that proxy is in the post-execution state.
        /// </summary>
        private void RequirePostExecutionPhase()
        {
            if (!this.MethodWasCalled)
                throw new Exception("Method returned value for caching is not available until after method was called.");
        }

        #endregion Utility methods
    }
}
