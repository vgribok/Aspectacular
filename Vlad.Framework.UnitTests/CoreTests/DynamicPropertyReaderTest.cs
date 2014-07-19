#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    ///     Summary description for DynamicPropertyReaderTest
    /// </summary>
    [TestClass]
    public class DynamicPropertyReaderTest
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
        public void TestDynamicProeprtyReaderPerformance()
        {
            string testString = "Hello.";

            const int secondsToRun = 1;

            long runsPerSec = RunCounter.SpinPerSec(secondsToRun*1000, () => testString.GetPropertyValue<int>("Length"));
            long actualRunsPerSec = (long)(runsPerSec/(double)secondsToRun);
            long expectedBaseLine = 2450000;

            this.TestContext.WriteLine("Ran dynamic property accessor {0:#,#0} per second, expected {1:#,#0}.", actualRunsPerSec, expectedBaseLine);
        }
    }
}