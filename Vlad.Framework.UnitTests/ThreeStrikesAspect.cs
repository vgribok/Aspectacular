using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Aspectacular;

namespace Value.Framework.UnitTests
{
    /// <summary>
    /// Makes three attempts to call intercepted method if it throws and exception.
    /// </summary>
    public class ThreeStrikesAspect : Aspect
    {
        public override void Step_4_Optional_AfterCatchingMethodExecException()
        {
            if (this.Context.AttemptsMade < 3)
            {
                this.Context.ShouldRetryCall = true;
                Debug.WriteLine("Retrying call for the {0} time due to \"{1}\".",  this.Context.AttemptsMade+1, this.Context.MethodExecutionException.Message);
            }
        }
    }
}
