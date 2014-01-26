using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Aspectacular
{
    /// <summary>
    /// Aspect for measuring baseline performance of this AOP framework.
    /// </summary>
    internal class DoNothingPerfTestAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            //string sign = this.Context.InterceptedCallMetaData.GetMethodSignature();
            //sign.ToString();
        }
    }

    /// <summary>
    /// Retrieves method signature with parameter values.
    /// It is very slow process, be careful.
    /// </summary>
    public class SlowFullMethodSignatureAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            string methodSign = this.Proxy.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);
            this.LogInformationWithKey("Method signature with parameters", methodSign);
        }
    }

    /// <summary>
    /// Adds value returned by intercepted method as log entry.
    /// Use with care: if returned value is a large data set, ToString() can be slow and take lots of memory.
    /// </summary>
    public class ReturnValueLoggerAspect : Aspect
    {
        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            string returnResultFormatted = this.Proxy.InterceptedCallMetaData.FormatReturnResult(this.Proxy.ReturnedValue, trueUI_falseInternal: false);
            this.LogInformationData("Returned value", returnResultFormatted);
        }
    }

    /// <summary>
    /// Writes log to the Debug output at the end of the lifecycle of the call.
    /// </summary>
    public class DebugOutputAspect : Aspect
    {
        public readonly EntryType entryTypeFilter;

        public DebugOutputAspect()
            : this(EntryType.Error | EntryType.Warning | EntryType.Info)
        {
        }

        public DebugOutputAspect(EntryType typeOfEntriesToOutput)
        {
            this.entryTypeFilter = typeOfEntriesToOutput;
        }

        protected IEnumerable<string> GetTextEntries(List<CallLogEntry> entries)
        {
            var q = from entry in entries
                    where (entry.What & this.entryTypeFilter) != 0
                    select entry.ToString();

            return q;
        }

        public override void Step_7_AfterEverythingSaidAndDone()
        {
            string output = this.GetLogText(this.GetTextEntries);

            if (!output.IsBlank())
                Debug.WriteLine(output);
        }
    }

    /// <summary>
    /// Writes log to the Trace output at the end of the lifecycle of the call.
    /// </summary>
    public class TraceOutputAspect : DebugOutputAspect
    {
        public TraceOutputAspect(EntryType typeOfEntriesToOutput)
            : base(typeOfEntriesToOutput)
        {
        }

        public TraceOutputAspect() : base()
        {
        }

        public override void Step_7_AfterEverythingSaidAndDone()
        {
            string output = this.GetLogText(this.GetTextEntries);

            if (!output.IsBlank())
                Trace.WriteLine(output);
        }
    }
}
