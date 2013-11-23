using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Aspectacular;

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
    }
}
