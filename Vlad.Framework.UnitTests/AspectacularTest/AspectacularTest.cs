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
            string actual = dal.RunAugmented<SomeTestClass, string>(ctx => () => ctx.GetDateString("whatevs"), TestAspects);
            Assert.AreEqual("whatevs 2/5/2010 12:00:00 AM", actual);

            actual = AOP.AllocRunDispose<SomeTestDisposable, string>(disp => () => disp.Echo("some text"), TestAspects);
            Assert.AreEqual("some text", actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNonMethodExpressionInterceptionFailure()
        {
            string actual = AOP.RunAugmented<SomeTestClass, string>(new SomeTestClass(), ctx => () => ctx.GetDateString("whatevs") + "123", TestAspects);
            actual.ToString();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInterceptedException()
        {
            var dal = new SomeTestClass(new DateTime(2010, 2, 5));
            dal.RunAugmented<SomeTestClass, bool>(ctx => () => ctx.ThrowFailure(), TestAspects);
        }

        [TestMethod]
        public void CallCounter()
        {
            //var dal = new DalContextBase();
            //int id = 123;
            
            //long count = RunCounter.Spin(10000, () => dal.Execute<string>(ctx => () => ctx.FakeBlMethod(id)));
            //count.ToString();
            //Trace.WriteLine();
        }
    }
}
