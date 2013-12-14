using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public enum LoggerWho
    {
        Proxy,
        Aspect,
        Method,
        Caller,
    }

    public enum EntryType
    {
        /// <summary>
        /// Error
        /// </summary>
        Red,

        /// <summary>
        /// Warning
        /// </summary>
        Yellow,

        /// <summary>
        /// Information
        /// </summary>
        Green
    }

    public class CallLogEntry
    {
        public LoggerWho Who { get; internal set; }
        public Type OptionalAspectType { get; internal set; }

        public EntryType What { get; internal set; }

        public string Key { get; internal set; }

        public string Message { get; internal set; }

        public override string ToString()
        {
            string entryAsText = string.Format("[{0}][{1}] [{2}] = {3}", 
                    this.What,
                    this.Who == LoggerWho.Aspect ? "Aspect " + this.OptionalAspectType.Name : this.Who.ToString(),
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
    /// Base class collecting execution text information from aspects and method itself.
    /// </summary>
    public class CallLifetimeLog
    {
        public readonly List<CallLogEntry> callLog = new List<CallLogEntry>();

        internal void AddLogEntry(LoggerWho who, EntryType entryType, string optionalKey, string format, params object[] args)
        {
            if(who == LoggerWho.Aspect)
                throw new ArgumentException("Parameter 'who = ' LoggerWho.Aspect cannot be used her. Use another method overload that takes Aspect class type.");

            this.AddEntryIntrenal(who, null, entryType, optionalKey, format, args);
        }

        internal void AddLogEntry(Aspect who, EntryType entryType, string optionalKey, string format, params object[] args)
        {
            if (who == null)
                throw new ArgumentNullException("who");

            this.AddEntryIntrenal(LoggerWho.Aspect, who.GetType(), entryType, optionalKey, format, args);
        }

        private void AddEntryIntrenal(LoggerWho who, Type optionalAspectType, EntryType entryType, string optionalKey, string format, params object[] args)
        {
            var entry = new CallLogEntry 
            { 
                Who = who, 
                Key = optionalKey, What = entryType, 
                Message = format.SmartFormat(args),
                OptionalAspectType = optionalAspectType,
            };

            this.callLog.Add(entry);
        }

        public string GetLogText(Func<List<CallLogEntry>, IEnumerable<CallLogEntry>> entrySelector = null)
        {
            return GetLogText(null, entrySelector);
        }

        public string GetLogText(string lineSeparator, Func<List<CallLogEntry>, IEnumerable<CallLogEntry>> entrySelector = null)
        {
            if(entrySelector == null)
                entrySelector = entries => entries;

            if (string.IsNullOrEmpty(lineSeparator))
                lineSeparator = Environment.NewLine;

            string text = string.Join(lineSeparator, entrySelector(this.callLog).Select(entry => entrySelector.ToString()));
            return text;
        }
    }
}
