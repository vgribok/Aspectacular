#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Diagnostics;

namespace Aspectacular
{
    /// <summary>
    ///     Aspect for measuring baseline performance of this AOP framework.
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
    ///     Retrieves method signature with parameter values.
    ///     It is very slow process, be careful.
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
    ///     Adds value returned by intercepted method as log entry.
    ///     Use with care: if returned value is a large data set, ToString() can be slow and take lots of memory.
    /// </summary>
    public class ReturnValueLoggerAspect : Aspect
    {
        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            string returnResultFormatted = this.Proxy.InterceptedCallMetaData.FormatReturnResult(this.Proxy.ReturnedValue, false);
            this.LogInformationData("Returned value", returnResultFormatted);
        }
    }

    /// <summary>
    ///     Writes log to the Debug output at the end of the lifecycle of the call.
    /// </summary>
    public class DebugOutputAspect : LogOutputAspectBase
    {
        public DebugOutputAspect()
            : this(EntryType.Error | EntryType.Warning | EntryType.Info)
        {
        }

        public DebugOutputAspect(EntryType typeOfEntriesToOutput, string optionalKey = null, bool writeAllEntriesIfKeyFound = false)
            : this(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKey)
        {
        }

        public DebugOutputAspect(EntryType typeOfEntriesToOutput, bool writeAllEntriesIfKeyFound, params string[] optionalKeys)
            : base(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKeys)
        {
        }

        protected override void Output(string output)
        {
            Debug.WriteLine(output);
        }
    }

    /// <summary>
    ///     Writes log to the Trace output at the end of the lifecycle of the call.
    /// </summary>
    public class TraceOutputAspect : LogOutputAspectBase
    {
        public TraceOutputAspect()
            : this(EntryType.Error | EntryType.Warning | EntryType.Info)
        {
        }

        public TraceOutputAspect(EntryType typeOfEntriesToOutput, string optionalKey = null, bool writeAllEntriesIfKeyFound = false)
            : this(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKey)
        {
        }

        public TraceOutputAspect(EntryType typeOfEntriesToOutput, bool writeAllEntriesIfKeyFound, params string[] optionalKeys)
            : base(typeOfEntriesToOutput, writeAllEntriesIfKeyFound, optionalKeys)
        {
        }

        protected override void Output(string output)
        {
            Trace.WriteLine(output);
        }
    }
}