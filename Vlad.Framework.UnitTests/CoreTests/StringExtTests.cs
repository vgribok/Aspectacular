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

        [TestMethod]
        public void TestNonEmptyString()
        {
            NonEmptyString nbs = null;
            Assert.IsNull(nbs);
            Assert.IsTrue(nbs == null);
            Assert.AreEqual("Empty", nbs.ToStringEx("Empty"));
            Assert.IsNull(nbs.ToStringEx(null));

            nbs = string.Empty;
            Assert.IsNotNull(nbs);
            Assert.IsTrue(nbs == null);

            nbs = "Hello!";
            Assert.IsTrue(nbs == "Hello!");
            Assert.AreEqual(nbs, "Hello!");
            Assert.IsTrue(nbs.CompareTo("abc") > 0);
        }
    }
}
