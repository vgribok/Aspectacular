#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Threading;

namespace Aspectacular
{
    /// <summary>
    ///     Interface to be inherited by augmented objects that want to be interception-context-aware,
    ///     but not necessarily aspects.
    /// </summary>
    public interface IInterceptionContext
    {
        Proxy Proxy { get; set; }
    }

    /// <summary>
    ///     Interface to be inherited by augmented objects that want to be their own aspects.
    /// </summary>
    /// <remarks>
    ///     It's a little weird notion, but an intercepted object can be its own aspect.
    ///     If intercepted object implements this interface, it will be placed in the
    ///     collection of aspects during the call. It won't have direct access to the proxy though.
    /// </remarks>
    public interface IAspect
    {
        void Step_1_BeforeResolvingInstance();
        void Step_2_BeforeTryingMethodExec();
        void Step_3_BeforeMassagingReturnedResult();
        void Step_4_Optional_AfterSuccessfulCallCompletion();
        void Step_4_Optional_AfterCatchingMethodExecException();
        void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded);
        void Step_6_Optional_AfterInstanceCleanup();
        void Step_7_AfterEverythingSaidAndDone();
    }

    /// <summary>
    ///     Base class for all method interceptors
    /// </summary>
    public abstract class Aspect : IAspect
    {
        /// <summary>
        ///     Should be set when application is exiting.
        /// </summary>
        [Obsolete("Please use Threading.ApplicationExiting and Threading.Sleep() instead.")] public static ManualResetEvent ApplicationExiting = new ManualResetEvent(false);

        /// <summary>
        ///     AOP proxy intercepting current call.
        /// </summary>
        public virtual Proxy Proxy { get; set; }

        static Aspect()
        {
#pragma warning disable 618
            AppDomain.CurrentDomain.DomainUnload += (domainRaw, evt) => ApplicationExiting.Set();
#pragma warning restore 618
        }

        /// <summary>
        ///     Called for non-static methods only.
        ///     Please note that method metadata is not available at this point.
        /// </summary>
        public virtual void Step_1_BeforeResolvingInstance()
        {
        }

        /// <summary>
        ///     Called right before intercepted method execution.
        /// </summary>
        public virtual void Step_2_BeforeTryingMethodExec()
        {
        }

        /// <summary>
        ///     Called after intercepted method returned result and
        ///     before interceptor augmented it, usually by LINQ modifiers like List().
        ///     May be called multiple times if retries are enabled.
        /// </summary>
        /// <remarks>
        ///     LINQ's List(), Single(), etc. methods may be used to execute
        ///     query returned by the intercepted method. This interceptor
        ///     is called after query was returned and before it was executed.
        ///     During this period, Context.ReturnedValue has exact value
        ///     returned by the intercepted method.
        ///     After this interceptor is called, Context.ReturnedValue may be changed,
        ///     primarily by LINQ modifiers, like List().
        /// </remarks>
        public virtual void Step_3_BeforeMassagingReturnedResult()
        {
        }

        /// <summary>
        ///     Called after method itself and optional result massager were called successfully.
        ///     May be called multiple times when retries are enabled.
        /// </summary>
        /// <remarks>
        ///     Since this method may be called multiple times on retries,
        ///     put all finalization/cleanup logic into steps 5-7.
        /// </remarks>
        public virtual void Step_4_Optional_AfterSuccessfulCallCompletion()
        {
        }

        /// <summary>
        ///     Called after method execution failed (thrown an exception).
        /// </summary>
        public virtual void Step_4_Optional_AfterCatchingMethodExecException()
        {
        }

        /// <summary>
        ///     Called after method execution success or failure.
        /// </summary>
        public virtual void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
        }

        /// <summary>
        ///     Called only for instance method that have instance cleanup
        /// </summary>
        public virtual void Step_6_Optional_AfterInstanceCleanup()
        {
        }

        /// <summary>
        ///     The very final cutpoint in the life cycle of the call.
        /// </summary>
        public virtual void Step_7_AfterEverythingSaidAndDone()
        {
        }

        #region Utility Methods

        /// <summary>
        ///     This method can be used by caching aspects to supply return value without calling method itself.
        /// </summary>
        /// <param name="newReturnValue"></param>
        protected void CancelInterceptedMethodCallAndSetReturnValue(object newReturnValue)
        {
            if(this.Proxy.MethodWasCalled)
                throw new Exception("Invalid attempt to cancel intercepted method call after it was called.");

            this.Proxy.ReturnedValue = newReturnValue;
            this.Proxy.CancelInterceptedMethodCall = true;
        }

        #endregion Utility Methods

        #region Logging methods

        /// <summary>
        ///     Adds entry to the log held by the proxy.
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(EntryType entryType, string optionalKey, string format, params object[] args)
        {
            this.Proxy.AddLogEntry(this, entryType, optionalKey, format, args);
        }

        /// <summary>
        ///     Shortcut for logging information entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformationWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Info, optionalKey, format, args);
        }

        /// <summary>
        ///     Shortcut for logging information entries.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformation(string format, params object[] args)
        {
            this.LogInformationWithKey(null, format, args);
        }

        /// <summary>
        ///     Shortcut for logging information entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="data"></param>
        protected void LogInformationData(string optionalKey, object data)
        {
            this.LogInformationWithKey(optionalKey, data.ToStringEx());
        }

        /// <summary>
        ///     Shortcut for logging error entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogErrorWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Error, optionalKey, format, args);
        }

        /// <summary>
        ///     Shortcut for logging error entries.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            this.LogErrorWithKey(null, format, args);
        }

        /// <summary>
        ///     Shortcut for logging warning entries.
        /// </summary>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarningWithKey(string optionalKey, string format, params object[] args)
        {
            this.Log(EntryType.Warning, optionalKey, format, args);
        }

        /// <summary>
        ///     Shortcut for logging warning entries.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            this.LogWarningWithKey(null, format, args);
        }

        /// <summary>
        ///     Generates log text from a collection of log entries specified by entrySelector delegate.
        ///     If entrySelector is null, all entries are used generate log text.
        ///     Environment.NewLine as line separator.
        /// </summary>
        /// <param name="entrySelector">
        ///     Optional entry log filter delegate that may use entry.ToString() or its own logic to
        ///     generate text for each selected log entry.
        /// </param>
        /// <returns>Log text</returns>
        public string GetLogText(Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
        {
            return this.GetLogText(null, entrySelector);
        }

        /// <summary>
        ///     Generates log text from a collection of log entries specified by entrySelector delegate.
        ///     If entrySelector is null, all entries are used generate log text.
        ///     Environment.NewLine as line separator.
        /// </summary>
        /// <param name="lineSeparator"></param>
        /// <param name="entrySelector">
        ///     Optional entry log filter delegate that may use entry.ToString() or its own logic to
        ///     generate text for each selected log entry.
        /// </param>
        /// <returns></returns>
        public string GetLogText(string lineSeparator, Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
        {
            return this.Proxy.GetLogText(lineSeparator, entrySelector);
        }

        /// <summary>
        ///     Returns the worst type of entry found in the log entry collection.
        ///     For example, if only Info entries are there, then Info will be returned,
        ///     but if both Error and Info entries are present, Error will be returned.
        /// </summary>
        public EntryType? WorstEntryType
        {
            get { return this.Proxy.WorstEntryType; }
        }

        /// <summary>
        ///     Returns bitwise mix of all entry types in the log entry collection
        /// </summary>
        public EntryType? PresentEntryTypes
        {
            get { return this.Proxy.PresentEntryTypes; }
        }

        #endregion Logging methods

        /// <summary>
        ///     Default application-wide set of aspects supplied by DefaultAspectFactory delegate and GlobalAspects collection.
        ///     Default set aspects, if not empty, is always added first to all proxy aspect collections.
        /// </summary>
        public static IEnumerable<Aspect> DefaultAspects
        {
            get
            {
                IEnumerable<Aspect> configAspects = DefaultAspectsConfigSection.GetConfigAspects();

                if(DefaultAspectFactory != null)
                    configAspects = configAspects.SmartUnion(DefaultAspectFactory());

                return configAspects;
            }
        }

        /// <summary>
        ///     Delegate to a global aspect factory.
        ///     By default it's DefaultAspectsConfigSection.GetConfigAspects() loading
        ///     default aspects from .config file's DefaultAspectsConfigSection, if it's present.
        /// </summary>
        public static Func<IEnumerable<Aspect>> DefaultAspectFactory { get; set; }
    }
}