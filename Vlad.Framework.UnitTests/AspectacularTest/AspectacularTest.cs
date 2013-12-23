using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Core;
using Value.Framework.Aspectacular;
using Value.Framework.Aspectacular.Aspects;
using System.Collections.Generic;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class AspectacularTest
    {
        public static readonly StupidSimpleInProcCache testInProcCache = new StupidSimpleInProcCache();

        public TestContext TestContext { get; set; }

        public static Aspect[] MainAspectsWithNoDebug
        {
            get
            {
                return new Aspect[]
                {
                     new ThreeStrikesAspect(),
                     new CacheAspect<StupidSimpleInProcCache>(testInProcCache),
                     new LinqToSqlAspect(),
                     new ReturnValueLoggerAspect(),
                     new SlowFullMethodSignatureAspect(),
                };
            }
        }

        public static Aspect[] DebugAspects
        {
            get
            {
                return new Aspect[]
                {
                    new DebugOutputAspect(/*EntryType.Error | EntryType.Warning*/),
                };
            }
        }

        public static IEnumerable<Aspect> TestAspects
        {
            get
            {
                return DebugAspects.Union(MainAspectsWithNoDebug);
            }
        }

        [TestMethod]
        public void TestOne()
        {
            var dal = new SomeTestClass(new DateTime(2010, 2, 5));

            // Example of the most common use case: calling instance GetDateString() method returning string.
            string actual = dal.GetProxy(TestAspects).Invoke(instance => instance.GetDateString("whatevs"));
            
            Assert.AreEqual("whatevs 2/5/2010 12:00:00 AM", actual);

            // Example of instantiating an IDisposable class, calling its instance method returning string, and disposing of class instance.
            actual = AOP.GetAllocDisposeProxy<SomeTestDisposable>(TestAspects).Invoke(dispInstance => dispInstance.Echo("some text"));
            
            Assert.AreEqual("some text", actual);
        }

        public int IntProp { get; set; }

        [TestMethod]
        public void TestMethodMetadata()
        {
            int intParm = 456;
            this.IntProp = intParm;
            string refString = DateTime.Now.Ticks.ToString();
            bool outBool = false;
            SomeTestClass obj = new SomeTestClass();

            // Example of calling static void method.
            AOP.Invoke(TestAspects, () => SomeTestClass.MiscParmsStatic(this.IntProp, obj, ref refString, out outBool));
            Assert.IsTrue(outBool);

            System.Threading.Thread.Sleep(100);

            intParm = 12456;
            IntProp = intParm;
            refString = DateTime.Now.Ticks.ToString();
            obj = new SomeTestClass(new DateTime(1999, 5, 3));
            
            AOP.Invoke(TestAspects, () => SomeTestClass.MiscParmsStatic(this.IntProp, obj, ref refString, out outBool));
            Assert.IsTrue(outBool);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestNonMethodExpressionInterceptionFailure()
        {
            var someCls = new SomeTestClass();
            
            // Example of improper calling instance method returning string, by using a non-method-call operator.
            string actual = someCls.GetProxy(TestAspects).Invoke(instance => instance.GetDateString("whatevs") + "123");
            
            actual.ToString();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void TestInterceptedException()
        {
            var someCls = new SomeTestClass(new DateTime(2010, 2, 5));
            
            bool neverGetHere = someCls.GetProxy(TestAspects).Invoke(instance => instance.ThrowFailure());
        }

        [TestMethod]
        public void TestMethodSignatures()
        {
            var obj = new SomeTestClass();

            string username, password;

            username = "one";
            password = "password1";
            obj.GetProxy(MainAspectsWithNoDebug).Invoke(inst => inst.FakeLogin(username, password));

            obj = new SomeTestClass(new DateTime(2010, 11, 5));
            //username = "two";
            //password = "password2";
            obj.GetProxy(MainAspectsWithNoDebug).Invoke(inst => inst.FakeLogin(username, password));

            int index = "Wassup".GetProxy(TestAspects).Invoke(str => str.IndexOf('u'));
            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public void TestDefaultDebugAspect()
        {
            int index = "Wassup".GetProxy().Invoke(str => str.IndexOf('u'));
            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public void TestEmptyAspectCollection()
        {
            AOP.Invoke(null, () => DateTime.IsLeapYear(2012));
        }
    }
}
