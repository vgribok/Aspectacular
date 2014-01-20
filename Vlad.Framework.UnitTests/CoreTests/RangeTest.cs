using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for RangeTest
    /// </summary>
    [TestClass]
    public class RangeTest
    {
        public RangeTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void DoRangeTest()
        {
            var stringRange = RangeFactory.CreateRange(null, "cdf");
            string actual = stringRange.ToString();
            Assert.AreEqual("{ NULL : cdf }", actual);
            Assert.IsTrue(stringRange.Contains("abc"));
            Assert.IsFalse(stringRange.Contains("xyz"));

            stringRange = RangeFactory.CreateRange("xyz", "abc");
            actual = stringRange.ToString();
            Assert.AreEqual("{ abc : xyz }", actual);
            Assert.IsTrue(stringRange.Contains("abc"));
            Assert.IsTrue(stringRange.Contains("cfdes"));
            Assert.IsTrue(stringRange.Contains("xyz"));
            Assert.IsFalse(stringRange.Contains("aa"));
            Assert.IsFalse(stringRange.Contains("zz"));

            var intRange = RangeFactory.CreateRange<int>(1, null);
            actual = intRange.ToString();
            Assert.AreEqual("{ 1 : NULL }", actual);
            Assert.IsTrue(intRange.Contains(2));
            Assert.IsTrue(intRange.Contains(int.MaxValue));
            Assert.IsFalse(intRange.Contains(int.MinValue));
        }

        [TestMethod]
        public void EndOfDayTest()
        {
            var dt = DateTime.Now;
            Assert.AreEqual(dt.StartOfDay(), dt.EndOfDay().StartOfDay());

            dt = DateTime.Now.Date;
            Assert.AreEqual(dt.StartOfDay(), dt.EndOfDay().StartOfDay());
        }
    }
}
