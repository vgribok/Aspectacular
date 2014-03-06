using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aspectacular;

namespace Aspectacular.Test
{
    public class TimetampsAspect : Aspect
    {
        public bool UseUtc { get; protected set; }

        public TimetampsAspect()
        {
        }

        /// <summary>
        /// Parameter should be in the format of "useUtc=true;"
        /// </summary>
        /// <param name="configParams"></param>
        public TimetampsAspect(string configParams)
        {
            string useUtcStr = DefaultAspect.GetParameterValue(configParams, "Use Utc", "false");
            this.UseUtc = bool.Parse(useUtcStr);
        }

        protected DateTime GetCurrent()
        {
            return this.UseUtc ? DateTime.UtcNow : DateTime.Now;
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationData("Timestamp type", this.UseUtc ? "UTC time" : "Local time");
            this.LogInformationData("Timestamp for Step_2_BeforeTryingMethodExec", this.GetCurrent());
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            this.LogInformationData("Timestamp for Step_5_FinallyAfterMethodExecution", this.GetCurrent());
        }
    }
}
