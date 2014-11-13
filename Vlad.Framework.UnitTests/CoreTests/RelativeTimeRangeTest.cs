#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    ///     Summary description for RelativeTimeRangeTest
    /// </summary>
    [TestClass]
    public class RelativeTimeRangeTest
    {
        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
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
            DateRange range = TimeUnits.Month.Current(localNow.Add(-1, TimeUnits.Year));
            Assert.AreEqual(DateTimeKind.Unspecified, range.Kind);
        }

        [TestMethod]
        public void TestDateTimeStart()
        {
            DateTime dt = (new DateTime(2013, 12, 31, 0, 0, 0, DateTimeKind.Local)).EndOf(TimeUnits.Day);
            Assert.AreEqual(999, dt.Millisecond);

            DateRange dtr = TimeUnits.Month.Current(null);
            DateTime now = DateTime.Now;
            DateTime expectedStart = now.StartOf(TimeUnits.Month);
            DateTime expectedEnd = now.EndOf(TimeUnits.Month);
            Assert.AreEqual(expectedStart, dtr.Start.Value);
            Assert.AreEqual(expectedEnd, dtr.End.Value);

            string dtStr = dt.ToString(CultureInfo.InvariantCulture);
            Assert.AreEqual("12/31/2013 23:59:59", dtStr);

            DateTime actual = dt.StartOf(TimeUnits.Century);
            Assert.AreEqual("01/01/2000 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Decade);
            Assert.AreEqual("01/01/2010 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Year);
            Assert.AreEqual("01/01/2013 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Quarter);
            Assert.AreEqual("10/01/2013 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            for(int month = 1; month <= 12; month++)
            {
                DateTime qtrTest = new DateTime(2013, month, 5);
                int actualMonth = qtrTest.StartOf(TimeUnits.Quarter).Month;

                if(qtrTest.Month >= 1 && qtrTest.Month <= 3)
                    Assert.AreEqual(1, actualMonth);
                else if(qtrTest.Month >= 4 && qtrTest.Month <= 6)
                    Assert.AreEqual(4, actualMonth);
                else if(qtrTest.Month >= 7 && qtrTest.Month <= 9)
                    Assert.AreEqual(7, actualMonth);
                else if(qtrTest.Month >= 10 && qtrTest.Month <= 12)
                    Assert.AreEqual(10, actualMonth);
            }

            actual = dt.StartOf(TimeUnits.Week);
            Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek, actual.DayOfWeek);
            Assert.AreEqual("12/29/2013 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Month);
            Assert.AreEqual("12/01/2013 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            actual = dt.StartOf(TimeUnits.Day);
            Assert.AreEqual("12/31/2013 00:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Local, actual.Kind);

            dt = (new DateTime(2013, 12, 31, 0, 0, 0, DateTimeKind.Utc)).EndOf(TimeUnits.Day);

            actual = dt.StartOf(TimeUnits.Hour);
            Assert.AreEqual("12/31/2013 23:00:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);

            actual = dt.StartOf(TimeUnits.Minute);
            Assert.AreEqual("12/31/2013 23:59:00", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);

            actual = dt.StartOf(TimeUnits.Second);
            Assert.AreEqual("12/31/2013 23:59:59", actual.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(0, actual.Millisecond);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }


        [TestMethod]
        public void TestTimeMomentRangeMonth()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            TimeMomentRange dtr = TimeUnits.Month.Current((DateTimeOffset)new DateTime(2013, 5, 15, 11, 46, 38));

            DateTimeOffset expectedStart = now.StartOf(TimeUnits.Month);
            DateTimeOffset expectedEnd = now.EndOf(TimeUnits.Month);
            Assert.AreEqual(expectedStart, dtr.Start.Value);
            Assert.AreEqual(expectedEnd, dtr.End.Value);

            dtr = TimeUnits.Month.Current();
            expectedStart = now.StartOf(TimeUnits.Month);
            expectedEnd = now.EndOf(TimeUnits.Month);
            Assert.AreEqual(expectedStart, dtr.Start.Value);
            Assert.AreEqual(expectedEnd, dtr.End.Value);
        }

        [TestMethod]
        public void TestTimeMomentRangeYear()
        {
            DateTimeOffset now = DateTimeOffset.Now;

            TimeMomentRange dtr = TimeUnits.Year.Current((DateTimeOffset)new DateTime(now.Year, 5, 1, 11, 46, 38));

            DateTimeOffset expectedStart = now.StartOf(TimeUnits.Year);
            DateTimeOffset expectedEnd = now.EndOf(TimeUnits.Year);
            Assert.AreEqual(expectedStart, dtr.Start.Value);
            Assert.AreEqual(expectedEnd, dtr.End.Value);
        }

        [TestMethod]
        public void TestDateTimeOffsetDayLightSavingWhenAddingMonth()
        {
            DateTimeOffset someNov2014 = new DateTime(2014, 11, 12, 22, 28, 11, 0, DateTimeKind.Local);
            DateTimeOffset someDec2014 = someNov2014.AddMonths(1);
            DateTimeOffset dec2014Start = new DateTimeOffset(someDec2014.Year, someDec2014.Month, 1, 0, 0, 0, someNov2014.Offset);
            DateTimeOffset nov2014End = dec2014Start.PreviousMoment();
            DateTimeOffset nov2014Start = dec2014Start.AddMonths(-1);

            DateTimeOffset expectedStart = someNov2014.StartOf(TimeUnits.Month);
            DateTimeOffset expectedEnd = someNov2014.EndOf(TimeUnits.Month);

            Assert.AreEqual(expectedStart, nov2014Start);
            Assert.AreEqual(expectedEnd, nov2014End);
        }


        [TestMethod]
        public void TestTimeMomentStart()
        {
            TimeSpan localOffset = new TimeSpan(-5, 0, 0);

            DateTimeOffset dt = (new DateTimeOffset(2013, 12, 31, 0, 0, 0, localOffset)).EndOf(TimeUnits.Day);
            Assert.AreEqual(999, dt.Millisecond);

            string dtStr = dt.ToString();
            Assert.AreEqual("12/31/2013 11:59:59 PM -05:00", dtStr);

            DateTimeOffset actual = dt.StartOf(TimeUnits.Century);
            Assert.AreEqual("1/1/2000 12:00:00 AM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Decade);
            Assert.AreEqual("1/1/2010 12:00:00 AM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Year);
            Assert.AreEqual("1/1/2013 12:00:00 AM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Quarter);
            Assert.AreEqual("10/1/2013 12:00:00 AM -05:00", actual.ToString());

            for(int month = 1; month <= 12; month++)
            {
                DateTimeOffset qtrTest = new DateTimeOffset(2013, month, 5, 0, 0, 0, localOffset);
                int actualMonth = qtrTest.StartOf(TimeUnits.Quarter).Month;

                if(qtrTest.Month >= 1 && qtrTest.Month <= 3)
                    Assert.AreEqual(1, actualMonth);
                else if(qtrTest.Month >= 4 && qtrTest.Month <= 6)
                    Assert.AreEqual(4, actualMonth);
                else if(qtrTest.Month >= 7 && qtrTest.Month <= 9)
                    Assert.AreEqual(7, actualMonth);
                else if(qtrTest.Month >= 10 && qtrTest.Month <= 12)
                    Assert.AreEqual(10, actualMonth);
            }

            actual = dt.StartOf(TimeUnits.Week);
            Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek, actual.DayOfWeek);
            Assert.AreEqual("12/29/2013 12:00:00 AM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Month);
            Assert.AreEqual("12/1/2013 12:00:00 AM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Day);
            Assert.AreEqual("12/31/2013 12:00:00 AM -05:00", actual.ToString());

            dt = (new DateTimeOffset(2013, 12, 31, 0, 0, 0, localOffset)).EndOf(TimeUnits.Day);

            actual = dt.StartOf(TimeUnits.Hour);
            Assert.AreEqual("12/31/2013 11:00:00 PM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Minute);
            Assert.AreEqual("12/31/2013 11:59:00 PM -05:00", actual.ToString());

            actual = dt.StartOf(TimeUnits.Second);
            Assert.AreEqual("12/31/2013 11:59:59 PM -05:00", actual.ToString());
            Assert.AreEqual(0, actual.Millisecond);
        }
    }
}