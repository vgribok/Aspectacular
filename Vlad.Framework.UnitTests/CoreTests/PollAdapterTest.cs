using System;
using System.Text;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;

// ReSharper disable JoinDeclarationAndInitializer
namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for PollAdapterTest
    /// </summary>
    [TestClass]
    public class PollAdapterTest
    {
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
        public void PollSchedulingDelayTest()
        {
            const int twoSecDelay = 2 * 1000;

            DateTime nextCallTime;

            nextCallTime = BlockingPoll<int>.GetNextCallTimeUtcAlignedonDelayBoundary(twoSecDelay).ToLocalTime();
            Assert.IsTrue((nextCallTime.Second % 2) == 0 && nextCallTime.Millisecond == 0);

            const int fourHourDelay = 4 * 1000 * 60 * 60;
            nextCallTime = BlockingPoll<int>.GetNextCallTimeUtcAlignedonDelayBoundary(fourHourDelay).ToLocalTime();
            Assert.IsTrue((nextCallTime.Hour % 4) == 0 && nextCallTime.Millisecond == 0);


            for (int millisecBoundary = 1; millisecBoundary <= 11111; millisecBoundary++)
            {
                nextCallTime = BlockingPoll<int>.GetNextCallTimeUtcAlignedonDelayBoundary(millisecBoundary).ToLocalTime();

                DateTime now = DateTime.Now;
                Assert.IsTrue(/*(nextCallTime.Millisecond % millisecBoundary) == 0 &&*/ nextCallTime > now);
                int delayMillisec = (int)(nextCallTime - now).TotalMilliseconds;
                Assert.IsTrue(delayMillisec < millisecBoundary * 2 + 20);
            }
        }
    }
}
// ReSharper restore JoinDeclarationAndInitializer
