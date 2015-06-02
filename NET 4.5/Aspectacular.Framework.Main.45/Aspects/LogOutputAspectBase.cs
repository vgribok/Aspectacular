#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspectacular
{
    /// <summary>
    ///     Use this class as a base for creating loggers - subclasses that take
    ///     proxy log item collection and store them to whatever medium you need.
    ///     Override Output(string) method in your subclasses to write the log text
    ///     to the specific medium, Like Debug, Trace, log4net, Windows Event Log, etc.
    /// </summary>
    public abstract class LogOutputAspectBase : Aspect
    {
// ReSharper disable InconsistentNaming
        public readonly EntryType entryTypeFilter;
        public readonly string[] keys;
        public readonly bool writeAllEntriesIfKeyFound;
// ReSharper restore InconsistentNaming

        /// <summary>
        ///     Initializes log output base class
        /// </summary>
        /// <param name="typeOfEntriesToOutput">Desired combination of EntryType to filter items to be collected for outputting.</param>
        /// <param name="writeAllEntriesIfKeyFound">
        ///     If true and optionalKey is specified, the entire log collection is written if key is found in the collection.
        ///     If false and optionalKey is specified, only log items with the key will be written.
        /// </param>
        /// <param name="optionalKey">
        ///     Optional item keys to output or to decide whether log collection needs to be written to
        ///     output. All items are written if not specified.
        /// </param>
        protected LogOutputAspectBase(EntryType typeOfEntriesToOutput, bool writeAllEntriesIfKeyFound, IEnumerable<string> optionalKey)
        {
            this.keys = optionalKey == null ? new string[0] : optionalKey.Where(key => !key.IsBlank()).ToArray();

            if(writeAllEntriesIfKeyFound && keys.Length == 0)
                throw new ArgumentNullException("optionalKey parameter value must be specified when writeAllEntriesIfKeyFound = true.");

            this.entryTypeFilter = typeOfEntriesToOutput;
            this.writeAllEntriesIfKeyFound = writeAllEntriesIfKeyFound;
        }

        /// <summary>
        ///     Whittles down log item collection according with values of entryTypeFilter, keys, and writeAllEntriesIfKeyFound.
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        protected virtual IEnumerable<CallLogEntry> FilterLogEntries(List<CallLogEntry> entries)
        {
            IEnumerable<CallLogEntry> filtered;

            if(this.writeAllEntriesIfKeyFound && this.keys.Length != 0)
            {
                bool keyFound = entries.Any(entry => this.keys.Contains(entry.Key));
                if(!keyFound)
                    filtered = new CallLogEntry[0];
                else
                    filtered = entries.Where(entry => this.entryTypeFilter.IsAnyFlagOn(entry.What));
            } else
            {
                filtered = from entry in entries
                    where this.entryTypeFilter.IsAnyFlagOn(entry.What) && (this.keys.Length == 0 || this.keys.Contains(entry.Key))
                    select entry;
            }

            return filtered;
        }

        protected virtual IEnumerable<string> GetTextEntries(List<CallLogEntry> entries)
        {
            IEnumerable<string> retVal = this.FilterLogEntries(entries).Select(entry => entry.ToString());
            return retVal;
        }

        public override void Step_7_AfterEverythingSaidAndDone()
        {
            string output = this.GetLogText(this.GetTextEntries);

            if(!output.IsBlank())
                this.Output(output);
        }

        /// <summary>
        ///     Implement this method in the subclass to write actual log text to the destination.
        /// </summary>
        /// <param name="logText">Text representing the log collection of the intercepted call.</param>
        protected abstract void Output(string logText);
    }
}