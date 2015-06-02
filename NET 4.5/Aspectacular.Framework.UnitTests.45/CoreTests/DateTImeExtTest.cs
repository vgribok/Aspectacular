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
    ///     Summary description for DateTImeExtTest
    /// </summary>
    [TestClass]
    public class DateTImeExtTest
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
        public void TestQuarterCalc()
        {
            for(int month = 1; month <= 12; month++)
            {
                DateTime qtrTest = new DateTime(2013, month, 5);

                int quarter = qtrTest.Quarter();

                if(qtrTest.Month >= 1 && qtrTest.Month <= 3)
                    Assert.AreEqual(1, quarter);
                else if(qtrTest.Month >= 4 && qtrTest.Month <= 6)
                    Assert.AreEqual(2, quarter);
                else if(qtrTest.Month >= 7 && qtrTest.Month <= 9)
                    Assert.AreEqual(3, quarter);
                else if(qtrTest.Month >= 10 && qtrTest.Month <= 12)
                    Assert.AreEqual(4, quarter);
            }
        }

        [TestMethod]
        public void EndOfDayTest()
        {
            DateTime dt = DateTime.Now;
            Assert.AreEqual(dt.StartOf(TimeUnits.Day), dt.EndOf(TimeUnits.Day).StartOf(TimeUnits.Day));

            dt = DateTime.Now.Date;
            Assert.AreEqual(dt.StartOf(TimeUnits.Day), dt.EndOf(TimeUnits.Day).StartOf(TimeUnits.Day));
        }

        [TestMethod]
        public void TestWeekOfYear()
        {
            DateTime dt = new DateTime(2009, 1, 1);
            Assert.AreEqual(DayOfWeek.Thursday, dt.DayOfWeek);

            int week = dt.WeekOfYear(CalendarWeekRule.FirstFourDayWeek);
            Assert.AreEqual(53, week);

            week = dt.WeekOfYear(CalendarWeekRule.FirstFullWeek);
            Assert.AreEqual(52, week);

            week = dt.WeekOfYear(CalendarWeekRule.FirstDay);
            Assert.AreEqual(1, week);
        }

        [TestMethod]
        public void TestToSortableIntDate()
        {
            DateTime dt = new DateTime(1961, 04, 12, 16, 12, 1, 98, DateTimeKind.Utc);

            int actual = dt.ToSortableIntDate();
            Assert.AreEqual(19610412, actual);

            DateTime dactual = actual.FromSortableIntDateTime(dt.Kind);
            Assert.AreEqual(dt.StartOf(TimeUnits.Day), dactual);

            actual = dt.ToSortableIntTime();
            Assert.AreEqual(161201, actual);

            dactual = actual.FromSortableIntTime(dt);
            Assert.AreEqual(dt.StartOf(TimeUnits.Second), dactual);

            long lactual = dt.ToSortableLongTime(true);
            Assert.AreEqual(161201098, lactual);

            dactual = lactual.FromSortableIntTime(dt);
            Assert.AreEqual(dt, dactual);

            lactual = dt.ToSortableLongDateTime(true);
            Assert.AreEqual(19610412161201098, lactual);

            dactual = lactual.FromSortableIntDateTime(dt.Kind);
            Assert.AreEqual(dt, dactual);

            lactual = dt.ToSortableLongDateTime(false);
            Assert.AreEqual(19610412161201, lactual);

            dactual = lactual.FromSortableIntDateTime(dt.Kind);
            Assert.AreEqual(dt.StartOf(TimeUnits.Second), dactual);
        }
    }
}