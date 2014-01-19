using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;

namespace Aspectacular.CoreTests
{
    [TestClass]
    public class StringExtTests
    {
        [TestMethod]
        public void TestNullToString()
        {
            string nullStr = null;
            string actual = nullStr.ToStringEx("hahaha!");
            Assert.AreEqual("hahaha!", actual);
        }
    }
}
