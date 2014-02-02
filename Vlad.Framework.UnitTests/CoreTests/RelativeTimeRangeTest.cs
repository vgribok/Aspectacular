using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

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
            DateTime localNow = new DateTime(2013, 9, 5); //DateTime.Now;

            DateTime refMoment = localNow.GoTo(dt => dt.Month == 9 && dt != localNow, dt => dt.AddMonths(-1)); // Finds past September;
            Assert.IsTrue(refMoment.Month == 9 && (localNow.Month > 9 || refMoment.Year < localNow.Year));
            Assert.AreEqual(refMoment, new DateTime(2012, 9, 5));

            DateRange range = LocalTimeUnits.Month.Current(refMoment);
            Assert.IsFalse(range.Kind == DateTimeKind.Utc);
        }

        [TestMethod]
        public void TestTimeUnitStart()
        {
            DateTime dt = (new DateTime(2013, 12, 31, 0, 0, 0, DateTimeKind.Local)).EndOf(TimeUnits.Day);
            Assert.AreEqual(999, dt.Millisecond);

            string dtStr = dt.ToString();
            Assert.AreEqual("12/31/2013 11:59:59 PM", dtStr);

            DateTime actual = dt.StartOf(TimeUnits.Century);
            Assert.AreEqual("1/1/2000 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Decade);
            Assert.AreEqual("1/1/2010 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Year);
            Assert.AreEqual("1/1/2013 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Quarter);
            Assert.AreEqual("10/1/2013 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            for (int month = 1; month <= 12; month++)
            {
                DateTime qtrTest = new DateTime(2013, month, 5);
                int actualMonth = qtrTest.StartOf(TimeUnits.Quarter).Month;

                if (qtrTest.Month >= 1 && qtrTest.Month <= 3)
                    Assert.AreEqual(1, actualMonth);
                else if (qtrTest.Month >= 4 && qtrTest.Month <= 6)
                    Assert.AreEqual(4, actualMonth);
                else if (qtrTest.Month >= 7 && qtrTest.Month <= 9)
                    Assert.AreEqual(7, actualMonth);
                else if (qtrTest.Month >= 10 && qtrTest.Month <= 12)
                    Assert.AreEqual(10, actualMonth);
            }

            actual = dt.StartOf(TimeUnits.Week);
            Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek, actual.DayOfWeek);
            Assert.AreEqual("12/29/2013 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Month);
            Assert.AreEqual("12/1/2013 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Day);
            Assert.AreEqual("12/31/2013 12:00:00 AM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);


            actual = dt.StartOf(TimeUnits.Hour);
            Assert.AreEqual("12/31/2013 11:00:00 PM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);

            actual = dt.StartOf(TimeUnits.Minute);
            Assert.AreEqual("12/31/2013 11:59:00 PM", actual.ToString());
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);

            actual = dt.StartOf(TimeUnits.Second);
            Assert.AreEqual("12/31/2013 11:59:59 PM", actual.ToString());
            Assert.AreEqual(0, actual.Millisecond);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }
    }
}
