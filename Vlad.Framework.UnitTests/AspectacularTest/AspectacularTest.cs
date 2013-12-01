using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Core;
using Value.Framework.Aspectacular;

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
            string actual = dal.GetProxy(TestAspects).Invoke(instance => instance.GetDateString("whatevs"));
            Assert.AreEqual("whatevs 2/5/2010 12:00:00 AM", actual);

            actual = AOP.AllocInvokeDispose<SomeTestDisposable, string>(TestAspects, disp => disp.Echo("some text"));
            Assert.AreEqual("some text", actual);
        }

        public int IntProp { get; set; }

        [TestMethod]
        public void TestMethodMetadata()
        {
            int intParm = 456;
            this.IntProp = intParm;
            string refString = DateTime.Now.ToString();
            bool outBool = false;

            AOP.Invoke(TestAspects, () => SomeTestClass.MiscParms(this.IntProp, ref refString, out outBool));
            Assert.IsTrue(outBool);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNonMethodExpressionInterceptionFailure()
        {
            var someCls = new SomeTestClass();
            string actual = someCls.GetProxy(TestAspects).Invoke(instance => instance.GetDateString("whatevs") + "123");
            actual.ToString();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestInterceptedException()
        {
            var someCls = new SomeTestClass(new DateTime(2010, 2, 5));
            someCls.GetProxy(TestAspects).Invoke(instance => instance.ThrowFailure());
        }
    }
}
