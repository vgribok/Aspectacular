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

        [TestMethod]
        public void CallCounter()
        {
            var dal = new SomeTestClass();
            int id = 123;
            Aspect[] aspects = { new DoNothingPerfTestAspect() };
            const int millisecToRun = 1000;
            const int baseLineRunsPerSec = 4000;
            long count = RunCounter.Spin(millisecToRun, () => dal.RunAugmented<SomeTestClass, int>(aspects, ctx => () => ctx.DoNothing(id)));
            long runsPerSec = count / (millisecToRun / 1000);
            Assert.IsTrue(runsPerSec >= baseLineRunsPerSec);
        }
    }
}
