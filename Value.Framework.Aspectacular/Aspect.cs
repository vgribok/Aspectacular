using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    /// <summary>
    /// Base class for all method interceptors
    /// </summary>
    public abstract class Aspect
    {
        public Interceptor Context { get; internal set; }

        public Aspect() { }

        /// <summary>
        /// Called for non-static methods only
        /// </summary>
        public virtual void Step_1_BeforeResolvingInstance() { }

        /// <summary>
        /// Called right before intercepted method execution.
        /// </summary>
        public virtual void Step_2_BeforeTryingMethodExec() { }

        /// <summary>
        /// Called after intercepted method returned result and 
        /// before interceptor augmented it, usually by LINQ modifiers like List().
        /// </summary>
        /// <remarks>
        /// LINQ's List(), Single(), etc. methods may be used to execute
        /// query returned by the intercepted method. This interceptor
        /// is called after query was returned and before it was executed.
        /// During this period, Context.MethodExecutionResult has exact value 
        /// returned by the intercepted method.
        /// After this interceptor is called, Context.MethodExecutionResult may be changed,
        /// primarily by LINQ modifiers, like List().
        /// </remarks>
        public virtual void Step_3_BeforeMassagingReturnedResult() { }

        /// <summary>
        /// Called after method execution failed (thrown an exception)
        /// </summary>
        public virtual void Step_4_Optional_AfterCatchingMethodExecException() { }

        /// <summary>
        /// Called after method execution success or failure.
        /// </summary>
        public virtual void Step_5_FinallyAfterMethodExecution() { }

        /// <summary>
        /// Called only for instance method that have instance cleanup 
        /// </summary>
        public virtual void Step_6_Optional_AfterInstanceCleanup() { }

        #region Utility Methods

        /// <summary>
        /// This method can be used by caching aspects to supply return value without calling method itself.
        /// </summary>
        /// <param name="newReturnValue"></param>
        protected void CancelInterceptedMethodCallAndSetReturnValue(object newReturnValue)
        {
            if (this.Context.methodCalled)
                throw new Exception("Invalid attempt to cancel intercepted method call after it was called.");

            this.Context.MethodExecutionResult = newReturnValue;
            this.Context.CancelInterceptedMethodCall = true;
        }

        #endregion Utility Methods
    }

    internal class DoNothingPerfTestAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
        }
    }

    public class DebugOutputAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            string methodSign = this.Context.InterceptedCallMetaData.GetMethodSignature();
            Debug.WriteLine("About to call method \"{0}\".".SmartFormat(methodSign));
        }

        //public override void Step_5_FinallyAfterMethodExecution()
        //{
        //    Debug.WriteLine("Method \"{0}\" {1}.".SmartFormat(
        //            this.Context.InterceptedCallMetaData.GetMethodSignature(),
        //            this.Context.MedthodHasFailed ? "failed" : "succeeded")
        //    );
        //}
    }
}
