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
    public class Proxy : CallLifetimeLog, IMethodLogProvider
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

        #endregion Limited fields and properties

        #region Public fields and properties

        /// <summary>
        /// Unique ID of the call.
        /// </summary>
        public readonly Guid CallID = Guid.NewGuid();

        /// <summary>
        /// Value returned by the intercepted method, if method call has succeeded.
        /// </summary>
        public object ReturnedValue { get; internal set; }

        /// <summary>
        /// An exception thrown either by the intercepted method, if method failed, or by preceding aspects.
        /// </summary>
        public Exception MethodExecutionException { get; set; }

        /// <summary>
        /// Extensive information about method, its name, attributes, parameter names and value, etc.
        /// </summary>
        public InterceptedMethodMetadata InterceptedCallMetaData { get; protected set; }

        /// <summary>
        /// Number of attempts made to call intercepted method.
        /// </summary>
        public byte AttemptsMade { get; private set; }

        /// <summary>
        /// Must be set by an aspect to indicate that failed intercepted call must be retried again.
        /// </summary>
        /// <remarks>
        /// Aspects should set this flag to true after encountering specific exceptions.
        /// </remarks>
        public bool ShouldRetryCall { get; set; }
        
        /// <summary>
        /// Can be set by an aspect to indicate that result was set from cache.
        /// </summary>
        public bool CancelInterceptedMethodCall { get; internal set; }

        /// <summary>
        /// Aspects may set this to true to break break aspect call sequence
        /// </summary>
        public bool StopAspectCallChain { get; set; }

        /// <summary>
        /// Ensures that method of a 3rd party class, called using this proxy instance will be treated
        /// as call-invariant, which makes such method potentially cacheable.
        /// </summary>
        /// <remarks>
        /// Call-invariance means that when the same method is called for two or more
        /// instances (or on the same class for static methods) at the same time,
        /// they will return same data.
        /// This flag only affects classes and methods that don't have InvariantReturnAttribute applied.
        /// This flag should be used only for classes whose source code cannot be modified
        /// by adding InvariantReturnAttribute to it, like .NET framework and other binary .NET components.
        /// </remarks>
        public bool ForceCallInvariance { get; set; }

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

        /// <summary>
        /// Is set to false until an attempt to call intercepted method was made,
        /// and is true after the method call attempt.
        /// </summary>
        public bool MethodWasCalled { get; protected set; }

        #endregion Public fields and properties

        #region Constructors

        public Proxy(Func<object> instanceFactory, Action<object> instanceCleaner, IEnumerable<Aspect> aspects)
        {
            this.LogInformation("#### Starting new call log ###");
            this.LogInformationData("Call ID", this.CallID);

            this.instanceResolverFunc = instanceFactory;
            this.instanceCleanerFunc = instanceCleaner;

            if (Aspect.DefaultAspects != null)
                aspects = aspects == null ? Aspect.DefaultAspects : Aspect.DefaultAspects.Union(aspects);

            aspects.ForEach(aspect => this.AddAspect(aspect));

            this.LogInformationData("Initial Aspects", string.Join(", ", this.aspects.Select(asp => asp.GetType().FormatCSharp())));
        }

        public Proxy(Func<object> instanceFactory, IEnumerable<Aspect> aspects)
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

            if (this.AugmentedClassInstance is ICallLogger)
                (this.AugmentedClassInstance as ICallLogger).AopLogger = this;

            // Augmented object can be interception context aware.
            if (this.AugmentedClassInstance is IInterceptionContext)
                ((IInterceptionContext)this.AugmentedClassInstance).Context = this;

            // Augmented object can be aspect for its own method interceptions.
            if (this.AugmentedClassInstance is IAspect)
                this.aspects.Add(this.AugmentedClassInstance as IAspect);

            this.LogInformationData("Type of resolved instance", this.AugmentedClassInstance.GetType().FormatCSharp());
        }

        protected virtual void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationData("Method signature", this.InterceptedCallMetaData.GetMethodSignature());
            this.LogInformationData("Intercepted method scope", this.InterceptedCallMetaData.IsStaticMethod ? "static" : "instance");
            this.LogInformationData("Method is call-invariant", this.CanCacheReturnedResult);

            this.CallAspects(aspect => aspect.Step_2_BeforeTryingMethodExec());
        }

        protected virtual void InvokeActualInterceptedMethod(Action interceptedMethodClosure)
        {
            interceptedMethodClosure.Invoke();
        }

        protected virtual void Step_3_BeforeMassagingReturnedResult()
        {
            this.LogInformationData("Pre-massaging returned result type", this.ReturnedValue == null ? "NULL" : this.ReturnedValue.GetType().FormatCSharp());
            this.CallAspects(aspect => aspect.Step_3_BeforeMassagingReturnedResult());
        }

        /// <summary>
        /// May be called multiple times for the same instance if call retry is enabled.
        /// </summary>
        protected virtual void Step_4_Optional_AfterCatchingMethodExecException()
        {
            this.CallAspects(aspect => aspect.Step_4_Optional_AfterCatchingMethodExecException());
        }

        protected virtual void Step_5_FinallyAfterMethodExecution()
        {
            this.LogInformationData("Call outcome", this.InterceptedMedthodCallFailed ? "failure" : "success");

            if (this.InterceptedMedthodCallFailed)
            {
                this.LogErrorWithKey("Main exception", this.MethodExecutionException.ToStringEx());
                this.LogErrorWithKey("Failed method", this.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue));
            }else
            {
                this.LogInformationData("Final returned result type", this.ReturnedValue == null ? "NULL" : this.ReturnedValue.GetType().FormatCSharp());
                //    // This can be slow if amount of data returned is large.
                //    this.LogInformationData("Returned value", this.InterceptedCallMetaData.FormatReturnResult(this.ReturnedValue, trueUI_falseInternal: false));
            }

            this.CallAspectsBackwards(aspect => aspect.Step_5_FinallyAfterMethodExecution(!this.InterceptedMedthodCallFailed));
        }

        protected virtual void Step_6_Optional_AfterInstanceCleanup()
        {
            this.LogInformation("Instance was cleaned up");

            this.CallAspectsBackwards(aspect => aspect.Step_6_Optional_AfterInstanceCleanup());
        }

        protected virtual void Step_7_AfterEverythingSaidAndDone()
        {
            this.LogInformation("**** Finished call with ID = {0} ****\r\n", this.CallID);

            this.CallAspectsBackwards(aspect => { aspect.Step_7_AfterEverythingSaidAndDone(); aspect.Context = null; });
        }


        #endregion Steps in sequence

        /// <summary>
        /// Method call wrapper that calls aspects and the intercepted method.
        /// </summary>
        /// <param name="actualMethodInvokerClosure">Intercepted method call wrapped in an interceptor's closure.</param>
        protected void ExecuteMainSequence(Action actualMethodInvokerClosure)
        {
            if (this.isUsed)
                throw new Exception("Same instance of the call interceptor cannot be used more than once.");

            this.isUsed = true;

            this.ReturnedValue = null;
            this.MethodExecutionException = null;
            this.MethodWasCalled = false;

            try
            {
                this.CheckForRequiredAspects();

                this.Step_2_BeforeTryingMethodExec();

                this.MethodWasCalled = true;

                try
                {
                    if(this.CancelInterceptedMethodCall)
                    {   // Returned result came from cache.
                        if(this.InterceptedMedthodCallFailed)
                        {   // Return cached exception.
                            this.Step_4_Optional_AfterCatchingMethodExecException();
                            throw this.MethodExecutionException;
                        }
                    }else
                    // Retry loop
                    for (this.AttemptsMade = 1; true; this.AttemptsMade++)
                    {   
                        try
                        {
                            actualMethodInvokerClosure.Invoke(); // Step 3 (post-call returned result massaging) is called inside this closure.
                            break; // success - break retry loop.
                        }
                        catch (Exception ex)
                        {
                            this.MethodExecutionException = ex;
                            this.Step_4_Optional_AfterCatchingMethodExecException();

                            if (this.ShouldRetryCall)
                                this.ShouldRetryCall = false;
                            else
                            {
                                // No more call attempts - break the retry loop.
                                if (ex == this.MethodExecutionException)
                                    throw;

                                throw this.MethodExecutionException;
                            }
                        }
                    } // retry loop
                }
                finally
                {
                    this.Step_5_FinallyAfterMethodExecution();
                }
            }
            finally
            {
                // Cleanup phase.
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

                this.Step_7_AfterEverythingSaidAndDone();
            }
        }

        #region Utility methods

        private void AddAspect(Aspect aspect, bool trueAppend_falseInsertFirst = true)
        {
            aspect.Context = this;

            if (trueAppend_falseInsertFirst)
                this.aspects.Add(aspect);
            else
                this.aspects.Insert(0, aspect);
        }

        private void CheckForRequiredAspects()
        {
            IEnumerable<RequiredAspectAttribute> requiredAspAttribs = this.InterceptedCallMetaData.GetMethodOrClassAttributes<RequiredAspectAttribute>();

            requiredAspAttribs.ForEach(requiredAspAttrib => this.EnsureRequiredAspect(requiredAspAttrib));
        }

        private void EnsureRequiredAspect(RequiredAspectAttribute requiredAspAttrib)
        {
            bool hasAspect = this.aspects.Where(asp => asp.GetType() == requiredAspAttrib.AspectClassType).Any();
            if (hasAspect)
                return;

            if (requiredAspAttrib.InstantiateIfMissing == WhenRequiredAspectIsMissing.DontInstantiate)
            {
                throw new Exception("Aspect {0} is required by \"{1}\", but is not in the caller's aspect collection.".SmartFormat(
                                                            requiredAspAttrib.AspectClassType.FormatCSharp(),
                                                            this.InterceptedCallMetaData.GetMethodSignature()
                                                        )
                                   );
            }

            Aspect missingAspect = requiredAspAttrib.InstantiateAspect();

            this.AddAspect(missingAspect, requiredAspAttrib.InstantiateIfMissing == WhenRequiredAspectIsMissing.InstantiateAndAppend);
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
            this.InterceptedCallMetaData = new InterceptedMethodMetadata(this.AugmentedClassInstance, callLambdaWrapper, this.ForceCallInvariance);
        }

        protected void CallReturnValuePostProcessor<TOut>(Func<TOut, object> retValPostProcessor, TOut retVal)
        {
            this.ReturnedValue = retVal;

            this.Step_3_BeforeMassagingReturnedResult();

            if (retValPostProcessor != null && this.ReturnedValue != null)
                this.ReturnedValue = retValPostProcessor(retVal);
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
                    retVal = this.ReturnedValue;
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
