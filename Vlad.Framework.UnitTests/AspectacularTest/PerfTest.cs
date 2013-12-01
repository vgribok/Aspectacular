using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Value.Framework.Aspectacular;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class PerfTest
    {
        const int millisecToRun = 2 * 1000;

        private Aspect[] doNothingAspects
        {
            get
            {
                return new Aspect[] 
                { 
                    new DoNothingPerfTestAspect() 
                };
            }
        }


        [TestMethod]
        public void CallConstPerfCounter()
        {
            const int baseLineConstParmRunsPerSec = 4000;

            var dal = new SomeTestClass();

            long runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => dal.GetProxy(doNothingAspects).Invoke(ctx => ctx.DoNothing(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineConstParmRunsPerSec);
        }

        [TestMethod]
        public void CallConstStaticPerfCounter()
        {
            const int baseLineMultiThreadConstStaticParmRunsPerSec = 23000; // 31500; // 33500;
            const int baseLineSingleThreadConstStaticParmRunsPerSec = 8500; // 9000

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(millisecToRun, () => AOP.Invoke(doNothingAspects, () => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineMultiThreadConstStaticParmRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => AOP.Invoke(doNothingAspects, () => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineSingleThreadConstStaticParmRunsPerSec);
        }

        [TestMethod]
        public void CallPerfCounter()
        {
            const int baseLineSingleThreadRunsPerSec = 2700;
            const int baseLineMultiThreadRunsPerSec = 9500; // 10000;

            var dal = new SomeTestClass();
            long runsPerSec;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = { 1, 2, 3, 4, 5 };

            runsPerSec = RunCounter.SpinParallelPerSec(millisecToRun, () => dal.GetProxy(doNothingAspects).Invoke(ctx => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineMultiThreadRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => dal.GetProxy(doNothingAspects).Invoke(ctx => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineSingleThreadRunsPerSec);
        }

        [TestMethod]
        public void CallPerfStaticCounter()
        {
            const int baseLineParallelRunsPerSec = 10000;
            const int baseLineRunsPerSec = 3300; // 3500;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = { 1, 2, 3, 4, 5 };

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(millisecToRun, () => AOP.Invoke(doNothingAspects, () => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineParallelRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => AOP.Invoke(doNothingAspects, () => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineRunsPerSec);
        }
    }
}
