using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public abstract class Aspect
    {
        public Interceptor Context { get; set; }

        public Aspect() { }

        public virtual void Step_1_BeforeResolvingInstance() { }

        public virtual void Step_2_BeforeTryingMethodExec() { }
        public virtual void Step_3_Optional_AfterCatchingMethodExecException() { }
        public virtual void Step_4_FinallyAfterMethodExecution() { }

        public virtual void Step_5_Optional_AfterInstanceCleanup() { }
    }

    internal class DoNothingPerfTestAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            //string sign = this.Context.InterceptedCallMetaData.GetMethodSignature();
            //sign.ToString();
        }
    }

    public class DebugOutputAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            string methodSign = this.Context.InterceptedCallMetaData.GetMethodSignature();
            Debug.WriteLine("About to call method \"{0}\".".SmartFormat(methodSign));
        }

        //public override void Step_4_FinallyAfterMethodExecution()
        //{
        //    Debug.WriteLine("Method \"{0}\" {1}.".SmartFormat(
        //            this.Context.InterceptedCallMetaData.GetMethodSignature(),
        //            this.Context.MedthodHasFailed ? "failed" : "succeeded")
        //    );
        //}
    }
}
