#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Web;

namespace Aspectacular
{
    /// <summary>
    ///     Outputs AOP call log records to ASP.NET Trace context.
    /// </summary>
    public class AspNetTraceAspect : LogOutputAspectBase
    {
        public AspNetTraceAspect()
            : this(EntryType.Error | EntryType.Warning | EntryType.Info)
        {
        }

        public AspNetTraceAspect(EntryType typeOfEntriesToOutput, string optionalKey = null,
            bool writeAllEntriesIfKeyFound = false)
            : this(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKey)
        {
        }

        public AspNetTraceAspect(EntryType typeOfEntriesToOutput, bool writeAllEntriesIfKeyFound,
            params string[] optionalKeys)
            : base(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKeys)
        {
        }

        protected override void Output(string output)
        {
            if(HttpContext.Current == null)
                return;

            EntryType? worstCase = this.WorstEntryType;

            if(worstCase == null)
                return;

            if(worstCase.Value == EntryType.Info)
                HttpContext.Current.Trace.Write(output);
            else
                HttpContext.Current.Trace.Warn(output);
        }
    }
}