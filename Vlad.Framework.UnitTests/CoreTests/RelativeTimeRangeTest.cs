using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for RelativeTimeRangeTest
    /// </summary>
    [TestClass]
    public class RelativeTimeRangeTest
    {
        public RelativeTimeRangeTest()
        {
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

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
        public void TestRelativeSpans()
        {
            // Previous September
            bool isUtc;
            DateTime localNow = new DateTime(2013, 9, 5); //DateTime.Now;

            DateTime refMoment = localNow.GoTo(dt => dt.Month == 9 && dt != localNow, dt => dt.AddMonths(-1)); // Finds past September;
            Assert.IsTrue(refMoment.Month == 9 && (localNow.Month > 9 || refMoment.Year < localNow.Year));
            Assert.AreEqual(refMoment, new DateTime(2012, 9, 5));

            DateRange range = LocalTimeUnits.Month.Current(out isUtc, refMoment);
            Assert.IsFalse(isUtc);
        }
    }
}
