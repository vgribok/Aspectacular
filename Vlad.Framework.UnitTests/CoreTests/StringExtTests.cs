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

            actual = "Привет, Вася!".ToLowerEx();
            Assert.AreEqual("привет, вася!", actual);

            actual = "Привет, Вася!".ToUpperInvariantEx();
            Assert.AreEqual("ПРИВЕТ, ВАСЯ!", actual);

            actual = "Fälla".ToLowerEx();
            Assert.AreEqual("fälla", actual);

            actual = "Fälla".ToLowerInvariantEx();
            Assert.AreEqual("fälla", actual);

            actual = nullStr.ToLowerEx();
            Assert.IsNull(actual);
        }
    }
}
