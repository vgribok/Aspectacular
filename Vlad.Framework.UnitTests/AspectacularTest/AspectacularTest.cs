using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Aspectacular;
using System.Diagnostics;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class AspectacularTest
    {
        public static Aspect[] TestAspects
        {
            get
            {
                return new Aspect[]
                {
                     new DebugOutputAspect(),
                };
            }
        }

        [TestMethod]
        public void TestOne()
        {
            var dal = new SomeTestClass(new DateTime(2010, 2, 5));
            string actual = dal.RunAugmented<SomeTestClass, string>(TestAspects, ctx => () => ctx.GetDateString("whatevs"));
            Assert.AreEqual("whatevs 2/5/2010 12:00:00 AM", actual);

            actual = AOP.AllocRunDispose<SomeTestDisposable, string>(TestAspects, disp => () => disp.Echo("some text"));
            Assert.AreEqual("some text", actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNonMethodExpressionInterceptionFailure()
        {
            var instance = new SomeTestClass();
            string actual = instance.RunAugmented<SomeTestClass, string>(TestAspects, ctx => () => ctx.GetDateString("whatevs") + "123");
            actual.ToString();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInterceptedException()
        {
            var dal = new SomeTestClass(new DateTime(2010, 2, 5));
            dal.RunAugmented<SomeTestClass, bool>(TestAspects, ctx => () => ctx.ThrowFailure());
        }

        const int millisecToRun = 2 * 1000;
        static readonly Aspect[] aspects = { new DoNothingPerfTestAspect() };

        [TestMethod]
        public void CallConstPerfCounter()
        {
            const int baseLineConstParmRunsPerSec = 4000;

            var dal = new SomeTestClass();

            long runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => dal.RunAugmented(aspects, ctx => () => ctx.DoNothing(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineConstParmRunsPerSec);
        }

        [TestMethod]
        public void CallConstStaticPerfCounter()
        {
            const int baseLineSingleThreadConstStaticParmRunsPerSec = 9000;
            const int baseLineMultiThreadConstStaticParmRunsPerSec = 33500;

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(millisecToRun, () => AOP.RunAugmented(aspects, () => () => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineMultiThreadConstStaticParmRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => AOP.RunAugmented(aspects, () => () => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            Assert.IsTrue(runsPerSec >= baseLineSingleThreadConstStaticParmRunsPerSec);
        }

        [TestMethod]
        public void CallPerfCounter()
        {
            const int baseLineSingleThreadRunsPerSec = 2700;
            const int baseLineMultiThreadRunsPerSec = 10000;

            var dal = new SomeTestClass();
            long runsPerSec;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = { 1, 2, 3, 4, 5 };

            runsPerSec = RunCounter.SpinParallelPerSec(millisecToRun, () => dal.RunAugmented(aspects, ctx => () => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineMultiThreadRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => dal.RunAugmented(aspects, ctx => () => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineSingleThreadRunsPerSec);
        }

        [TestMethod]
        public void CallPerfStaticCounter()
        {
            const int baseLineRunsPerSec = 3500;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = { 1, 2, 3, 4, 5 };

            long runsPerSec = RunCounter.SpinPerSec(millisecToRun, () => AOP.RunAugmented(aspects, () => () => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            Assert.IsTrue(runsPerSec >= baseLineRunsPerSec);
        }
    }
}
