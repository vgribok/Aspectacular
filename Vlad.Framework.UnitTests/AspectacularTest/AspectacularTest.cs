using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Aspectacular;
using System.Diagnostics;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class AspectacularTest
    {
        [TestMethod]
        public void TestOne()
        {
            var dal = new SomeTestClass(new DateTime(2010, 2, 5));
            string actual = dal.RunAugmented<SomeTestClass, string>(ctx => () => ctx.GetDateString("whatevs"));
            Assert.AreEqual("whatevs 2/5/2010 12:00:00 AM", actual);

            actual =  AOP.AllocRunDispose<SomeTestDisposable, string>(disp => () => disp.Echo("some text"));
            Assert.AreEqual("some text", actual);
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
