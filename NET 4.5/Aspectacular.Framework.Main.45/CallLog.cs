#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Aspectacular
{
    /// <summary>
    /// Specifies creator of a log entry in call's log collection
    /// </summary>
    public enum LogEntryOriginator
    {
        Proxy,
        Aspect,
        Method,
        Caller,
    }

    /// <summary>
    /// Specifies a type of a call log entry: an error, warning, etc.
    /// </summary>
    [Flags]
    public enum EntryType
    {
        /// <summary>
        ///     Error
        /// </summary>
        Error = 1,

        /// <summary>
        ///     Warning
        /// </summary>
        Warning = 2,

        /// <summary>
        ///     Information
        /// </summary>
        Info = 4,
    }

    /// <summary>
    ///     Implemented by proxy to give intercepted methods ability to log information that may be picked up by aspects.
    /// </summary>
    public interface IMethodLogProvider
    {
    }

    /// <summary>
    ///     If implemented by classes whose methods are intercepted,
    ///     then intercepted methods may use AOP logging via this.LogXXX();
    /// </summary>
    public interface ICallLogger
    {
        /// <summary>
        ///     An accessor to AOP logging functionality for intercepted methods.
        /// </summary>
        IMethodLogProvider AopLogger { get; set; }
    }

    /// <summary>
    /// Call log entry structure. AOP logger has a collection of these objects that is populated by Proxy, Aspects, and sometimes by callers and intercepted methods.
    /// </summary>
    public class CallLogEntry
    {
        /// <summary>
        /// Specifies the type of entry creator: whether it's Proxy, an aspect, caller, or intercepted method.
        /// </summary>
        public LogEntryOriginator Who { get; internal set; }

        /// <summary>
        /// If entry was created by an aspect, contains information about the type of the Aspect.
        /// </summary>
        public string OptionalAspectType { get; internal set; }

        /// <summary>
        /// Specifies what kind of log entry this is: an error, warning, etc.
        /// </summary>
        public EntryType What { get; internal set; }

        /// <summary>
        /// Log entry key. It's not required to be unique, but it might be a good idea to make it unique for structured logging.
        /// </summary>
        public string Key { get; internal set; }

        /// <summary>
        /// Log entry text.
        /// </summary>
        public string Message { get; internal set; }

        public override string ToString()
        {
            string entryAsText = string.Format("[{0}][{1}] [{2}] = \"{3}\"",
                this.What,
                this.Who == LogEntryOriginator.Aspect ? "Aspect:" + this.OptionalAspectType : this.Who.ToString(),
                this.Key.IsBlank() ? "MESSAGE" : this.Key,
                this.Message ?? "[NULL]"
                );
            return entryAsText;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }

    /// <summary>
    ///     Base class collecting entries with text information logged by aspects, proxy, callers and the intercepted method.
    /// </summary>
    public class CallLifetimeLog
    {
        /// <summary>
        /// If true, logging done by methods will be outputted to the System.Diagnostic.Trace
        /// if the methods are called w/o Aspectacular AOP.
        /// </summary>
        /// <remarks>
        /// When methods that log using ICallLogger or Proxy.CurrentLog are called without AOP Proxy,
        /// this flag allows this logging to go to Trace instead of being lost.
        /// Set this flag to false for backward compatibility - to have the behavior 
        /// as of before this change (where logs are thrown away).
        /// </remarks>
        public static bool FallbackToTraceLoggingWhenNoProxy = true;

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Collection of log entries populated during intercepted call lifecycle.
        /// It's populated by the Proxy, Aspects, and sometimes by the method itself or the caller.
        /// </summary>
        public readonly List<CallLogEntry> callLog = new List<CallLogEntry>();

        private CallerAopLogger callerLogAccessor = null;

        internal void AddLogEntry(LogEntryOriginator who, EntryType entryType, string category, string format, params object[] args)
        {
            if(who == LogEntryOriginator.Aspect)
                throw new ArgumentException("Parameter 'who = ' LoggerWho.Aspect cannot be used her. Use another method overload that takes Aspect class type.");

            this.AddEntryIntrenal(who, null, entryType, category, format, args);
        }

        internal void AddLogEntry(Aspect who, EntryType entryType, string category, string format, params object[] args)
        {
            if(who == null)
                throw new ArgumentNullException("who");

            this.AddEntryIntrenal(LogEntryOriginator.Aspect, who.GetType(), entryType, category, format, args);
        }

        private void AddEntryIntrenal(LogEntryOriginator who, Type optionalAspectType, EntryType entryType, string category, string format, params object[] args)
        {
            var entry = new CallLogEntry
            {
                Who = who,
                Key = category,
                What = entryType,
                Message = format.SmartFormat(args),
                OptionalAspectType = optionalAspectType == null ? null : optionalAspectType.FormatCSharp(true),
            };

            this.callLog.Add(entry);
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
        internal string GetLogText(string lineSeparator, Func<List<CallLogEntry>, IEnumerable<string>> entrySelector = null)
        {
            if(entrySelector == null)
                entrySelector = entries => entries.Select(entry => entry.ToString());

            if(string.IsNullOrEmpty(lineSeparator))
                lineSeparator = Environment.NewLine;

            string text = string.Join(lineSeparator, entrySelector(this.callLog));
            return text;
        }

        /// <summary>
        ///     Returns the worst type of entry found in the log entry collection.
        ///     For example, if only Info entries are there, then Info will be returned,
        ///     but if both Error and Info entries are present, Error will be returned.
        /// </summary>
        public EntryType? WorstEntryType
        {
            get
            {
                if(this.callLog.Count == 0)
                    return null;

                EntryType worstCase = (EntryType)this.callLog.Min(entry => (int)entry.What);
                return worstCase;
            }
        }

        /// <summary>
        ///     Returns bitwise mix of all entry types in the log entry collection
        /// </summary>
        public EntryType? PresentEntryTypes
        {
            get
            {
                if(this.callLog.Count == 0)
                    return null;

                List<EntryType> presentTypes = this.callLog.Select(entry => entry.What).Distinct().ToList();
                EntryType allTypes = presentTypes.First();
                presentTypes.ForEach(entryType => allTypes |= entryType);
                return allTypes;
            }
        }

        /// <summary>
        /// Provides access to AOP logging functionality for callers of AOP-intercepted functions.
        /// </summary>
        public CallerAopLogger CallerAopLogger
        {
            get { return this.callerLogAccessor ?? (this.callerLogAccessor = new CallerAopLogger(this)); }
        }

        #region Logging methods for the proxy

        /// <summary>
        ///     Adds log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(EntryType entryType, string category, string format, params object[] args)
        {
            this.AddLogEntry(LogEntryOriginator.Proxy, entryType, category, format, args);
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformationWithKey(string category, string format, params object[] args)
        {
            this.Log(EntryType.Info, category, format, args);
        }

        /// <summary>
        ///     Logs a piece of data as a log entry with key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        protected void LogInformationData(string key, object val)
        {
            this.Log(EntryType.Info, key, val.ToStringEx());
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogInformation(string format, params object[] args)
        {
            this.LogInformationWithKey(null, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogErrorWithKey(string category, string format, params object[] args)
        {
            this.Log(EntryType.Error, category, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            this.LogErrorWithKey(null, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarningWithKey(string category, string format, params object[] args)
        {
            this.Log(EntryType.Warning, category, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            this.LogWarningWithKey(null, format, args);
        }

        #endregion Logging methods for the proxy

        #region Hierarchical views of the log entry collection

        /// <summary>
        /// Log collection accessible by entry's key
        /// </summary>
        public IDictionary<string, IList<CallLogEntry>> KeyEntryLogHierarchy
        {
            get
            {
                IDictionary<string, IList<CallLogEntry>> dictionary = new Dictionary<string, IList<CallLogEntry>>();

                foreach(CallLogEntry entry in this.callLog)
                {
                    IList<CallLogEntry> entries;
                    string key = entry.Key ?? "[null]";
                    if(!dictionary.TryGetValue(key, out entries))
                    {
                        entries = new List<CallLogEntry>();
                        dictionary.Add(key, entries);
                    }
                    entries.Add(entry);
                }
                return dictionary;
            }
        }

        /// <summary>
        /// Log collection accessible by entry's type (Error, Info, etc.)
        /// </summary>
        public IDictionary<EntryType, IList<CallLogEntry>> EntryLogHierarchy
        {
            get
            {
                IDictionary<EntryType, IList<CallLogEntry>> dictionary = new Dictionary<EntryType, IList<CallLogEntry>>();

                foreach(CallLogEntry entry in this.callLog)
                {
                    IList<CallLogEntry> entries;
                    if(!dictionary.TryGetValue(entry.What, out entries))
                    {
                        entries = new List<CallLogEntry>();
                        dictionary.Add(entry.What, entries);
                    }
                    entries.Add(entry);
                }
                return dictionary;
            }
        }

        #endregion Hierarchical views of the log entry collection
    }

    /// <summary>
    ///     Holds logging methods accessible to intercepted methods if their parent class implements ICallLogger interface.
    /// </summary>
    public static class MethodCallLoggingExtensions
    {
        /// <summary>
        ///     Adds log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="entryType"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(this IMethodLogProvider methodLogger, EntryType entryType, string category, string format, params object[] args)
        {
            if(methodLogger == null)
            {
                if(CallLifetimeLog.FallbackToTraceLoggingWhenNoProxy)
                    FallbackTraceLog(entryType, format, args);
             
                return;
            }

            CallLifetimeLog log = (CallLifetimeLog)methodLogger;
            log.AddLogEntry(LogEntryOriginator.Method, entryType, category, format, args);
        }

        /// <summary>
        ///     Adds log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="entryType"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(this ICallLogger interceptedClass, EntryType entryType, string category, string format, params object[] args)
        {
            if(interceptedClass == null)
            {
                if (CallLifetimeLog.FallbackToTraceLoggingWhenNoProxy)
                    FallbackTraceLog(entryType, format, args);

                return;
            }

            interceptedClass.AopLogger.Log(entryType, category, format, args);
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogInformationWithKey(this IMethodLogProvider methodLogger, string category, string format, params object[] args)
        {
            Log(methodLogger, EntryType.Info, category, format, args);
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogInformationWithKey(this ICallLogger interceptedClass, string category, string format, params object[] args)
        {
            Log(interceptedClass, EntryType.Info, category, format, args);
        }

        /// <summary>
        ///     Logs a piece of data as a log entry with key.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public static void LogInformationData(this IMethodLogProvider methodLogger, string key, object val)
        {
            Log(methodLogger, EntryType.Info, key, val.ToStringEx());
        }

        /// <summary>
        ///     Logs a piece of data as a log entry with key.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public static void LogInformationData(this ICallLogger interceptedClass, string key, object val)
        {
            Log(interceptedClass, EntryType.Info, key, val.ToStringEx());
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogInformation(this IMethodLogProvider methodLogger, string format, params object[] args)
        {
            LogInformationWithKey(methodLogger, null, format, args);
        }

        /// <summary>
        ///     Adds information log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogInformation(this ICallLogger interceptedClass, string format, params object[] args)
        {
            LogInformationWithKey(interceptedClass, null, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogErrorWithKey(this IMethodLogProvider methodLogger, string category, string format, params object[] args)
        {
            Log(methodLogger, EntryType.Error, category, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogErrorWithKey(this ICallLogger interceptedClass, string category, string format, params object[] args)
        {
            Log(interceptedClass, EntryType.Error, category, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogError(this IMethodLogProvider methodLogger, string format, params object[] args)
        {
            LogErrorWithKey(methodLogger, null, format, args);
        }

        /// <summary>
        ///     Adds error log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for storing,
        ///     sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogError(this ICallLogger interceptedClass, string format, params object[] args)
        {
            LogErrorWithKey(interceptedClass, null, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogWarningWithKey(this IMethodLogProvider methodLogger, string category, string format, params object[] args)
        {
            Log(methodLogger, EntryType.Warning, category, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogWarningWithKey(this ICallLogger interceptedClass, string category, string format, params object[] args)
        {
            Log(interceptedClass, EntryType.Warning, category, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="methodLogger"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogWarning(this IMethodLogProvider methodLogger, string format, params object[] args)
        {
            LogWarningWithKey(methodLogger, null, format, args);
        }

        /// <summary>
        ///     Adds warning log entry to AOP Proxy log in a way that makes it possible for aspect classes to access it for
        ///     storing, sorting, grouping, etc.
        /// </summary>
        /// <param name="interceptedClass"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogWarning(this ICallLogger interceptedClass, string format, params object[] args)
        {
            LogWarningWithKey(interceptedClass, null, format, args);
        }

        private static void FallbackTraceLog(EntryType entryType, string format, params object[] args)
        {
            switch (entryType)
            {
                case EntryType.Error:
                    Trace.TraceError(format, args);
                    break;
                case EntryType.Warning:
                    Trace.TraceWarning(format, args);
                    break;
                case EntryType.Info:
                    Trace.TraceInformation(format, args);
                    break;
            }
        }
    }

    /// <summary>
    /// Provides access to logging functionality for callers of AOP-intercepted functions.
    /// </summary>
    public class CallerAopLogger
    {
        protected readonly CallLifetimeLog log;

        internal CallerAopLogger(CallLifetimeLog log)
        {
            this.log = log;
        }

        /// <summary>
        ///     Adds entry to the log held by the proxy.
        /// </summary>
        /// <param name="entryType"></param>
        /// <param name="optionalKey"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(EntryType entryType, string optionalKey, string format, params object[] args)
        {
            this.log.AddLogEntry(LogEntryOriginator.Caller, entryType, optionalKey, format, args);
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
    }
}