using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace Aspectacular
{
    /// <summary>
    /// Outputs AOP call log records to ASP.NET Trace context.
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
            if (HttpContext.Current == null)
                return;

            EntryType? worstCase = this.WorstEntryType;

            if (worstCase == null)
                return;

            if(worstCase.Value == EntryType.Info)
                HttpContext.Current.Trace.Write(output);
            else
                HttpContext.Current.Trace.Warn(output);
        }
    }
}
