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
    /// Interface to be inherited by augmented objects that want to be interception-context-aware,
    /// but not necessarily aspects.
    /// </summary>
    public interface IInterceptionContext
    {
        Proxy Context { get; set;  }
    }

    /// <summary>
    /// Interface to be inherited by augmented objects that want to be their own aspects.
    /// </summary>
    public interface IAspect
    {
        void Step_1_BeforeResolvingInstance();
        void Step_2_BeforeTryingMethodExec();
        void Step_3_BeforeMassagingReturnedResult();
        void Step_4_Optional_AfterCatchingMethodExecException();
        void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded);
        void Step_6_Optional_AfterInstanceCleanup();
        void Step_7_AfterEverythingSaidAndDone();
    }

    /// <summary>
    /// Base class for all method interceptors
    /// </summary>
    public abstract class Aspect : IAspect
    {
        public virtual Proxy Context { get; set; }

        static Aspect()
        {
            if (DefaultAspectFactory == null)
                DefaultAspectFactory = DefaultAspectsConfigSection.GetDefaultAspects;
        }

        public Aspect() { }

        /// <summary>
        /// Called for non-static methods only.
        /// Please note that method metadata is not available at this point.
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
        /// During this period, Context.ReturnedValue has exact value 
        /// returned by the intercepted method.
        /// After this interceptor is called, Context.ReturnedValue may be changed,
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
        public virtual void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded) { }

        /// <summary>
        /// Called only for instance method that have instance cleanup 
        /// </summary>
        public virtual void Step_6_Optional_AfterInstanceCleanup() { }

        /// <summary>
        /// The very final cutpoint in the life cycle of the call.
        /// </summary>
        public virtual void Step_7_AfterEverythingSaidAndDone() { }

        #region Utility Methods

        /// <summary>
        /// This method can be used by caching aspects to supply return value without calling method itself.
        /// </summary>
        /// <param name="newReturnValue"></param>
        protected void CancelInterceptedMethodCallAndSetReturnValue(object newReturnValue)
        {
            if (this.Context.MethodWasCalled)
                throw new Exception("Invalid attempt to cancel intercepted method call after it was called.");

            this.Context.ReturnedValue = newReturnValue;
            this.Context.CancelInterceptedMethodCall = true;
        }

        #endregion Utility Methods

        #region Logging methods

        /// <summary>
        /// Adds entry to the log held by the proxy.
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(EntryType entryType, string optionalKey, string format, params object[] args)
        {
            this.Context.AddLogEntry(this, entryType, optionalKey, format, args);
        }

        /// <summary>
        /// Shortcut for logging information entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformationWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Info, optionalKey, format, args);
        }

        /// <summary>
        /// Shortcut for logging information entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformation(string format, params object[] args)
        {
            this.LogInformationWithKey(null, format, args);
        }

        /// <summary>
        /// Shortcut for logging information entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="data"></param>
        protected void LogInformationData(string optionalKey, object data)
        {
            this.LogInformationWithKey(optionalKey, data.ToStringEx());
        }

        /// <summary>
        /// Shortcut for logging error entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogErrorWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Error, optionalKey, format, args);
        }

        /// <summary>
        /// Shortcut for logging error entries.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            this.LogErrorWithKey(null, format, args);
        }

        /// <summary>
        /// Shortcut for logging warning entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarningWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Warning, optionalKey, format, args);
        }

        /// <summary>
        /// Shortcut for logging warning entries.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            this.LogWarningWithKey(null, format, args);
        }

        /// <summary>
        /// Generates log text from a collection of log entries specified by entrySelector delegate.
        /// If entrySelector is null, all entries are used generate log text.
        /// Environment.NewLine as line separator.
        /// </summary>
        /// <param name="entrySelector">Optional entry log filter delegate that may use entry.ToString() or its own logic to generate text for each selected log entry.</param>
        /// <returns>Log text</returns>
        public string GetLogText(Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
        {
            return this.GetLogText(null, entrySelector);
        }

        /// <summary>
        /// Generates log text from a collection of log entries specified by entrySelector delegate.
        /// If entrySelector is null, all entries are used generate log text.
        /// Environment.NewLine as line separator.
        /// </summary>
        /// <param name="lineSeparator"></param>
        /// <param name="entrySelector">Optional entry log filter delegate that may use entry.ToString() or its own logic to generate text for each selected log entry.</param>
        /// <returns></returns>
        public string GetLogText(string lineSeparator, Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
        {
            return this.Context.GetLogText(lineSeparator, entrySelector);
        }

        #endregion Logging methods

        /// <summary>
        /// Default application-wide set of aspects supplied by DefaultAspectFactory delegate.
        /// Default set aspects, if not empty, is always added first to all proxies aspect collections.
        /// </summary>
        public static IEnumerable<Aspect> DefaultAspects 
        { 
            get { return DefaultAspectFactory == null ? null : DefaultAspectFactory(); } 
        }

        /// <summary>
        /// Delegate to a global aspect factory. 
        /// By default it's DefaultAspectsConfigSection.GetDefaultAspects() loading
        /// default aspects from .config file's DefaultAspectsConfigSection, if it's present.
        /// </summary>
        public static Func<IEnumerable<Aspect>> DefaultAspectFactory { get; set; }
    }
}
