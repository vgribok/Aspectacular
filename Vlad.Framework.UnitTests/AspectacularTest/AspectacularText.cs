using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Aspectacular;
using System.Diagnostics;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class AspectacularText
    {
        [TestMethod]
        public void TestOne()
        {
            var dal = new DalContext();
            int id = 123;
            string actual = dal.Execute<string>(ctx => () => ctx.FakeBlMethod(id));
            Assert.AreEqual("123", actual);
        }

        [TestMethod]
        public void CallCounter()
        {
            var dal = new DalContext();
            int id = 123;
            
            long count = RunCounter.Spin(10000, () => dal.Execute<string>(ctx => () => ctx.FakeBlMethod(id)));
            count.ToString();
            //Trace.WriteLine();
        }
    }
}
