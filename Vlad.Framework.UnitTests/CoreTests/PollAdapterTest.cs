using System;
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

        /// <summary>
        /// Keeps returning null while current time is 
        /// either less than target time, or exceeds it by 0.5 seconds.
        /// Otherwise returns current time.
        /// </summary>
        /// <param name="targetTime"></param>
        /// <returns></returns>
        public static object PollCurrentTime(DateTimeOffset targetTime)
        {
            var time = DateTimeOffset.Now;
            return time >= targetTime && (time - targetTime).Milliseconds <= 500 ? time : (object)null;
        }

        [TestMethod]
        public void TestSmartPollingBlocking()
        {
            DateTimeOffset threeSecondDelay = DateTimeOffset.Now.AddSeconds(3);

            const int maxDelayMillisec = 500;
            var pollmeister = new BlockingObjectPoll<object>(() => PollCurrentTime(threeSecondDelay), maxDelayMillisec);
            object result = pollmeister.WaitForPayload();
            this.TestContext.WriteLine("Empty poll calls: {0:#,#0}", pollmeister.EmptyPollCallCount);

            Assert.IsNotNull(result);
            int discrepMillisecBetweenHopedAndActual = ((DateTimeOffset)result - threeSecondDelay).Milliseconds;
            Assert.IsTrue(discrepMillisecBetweenHopedAndActual <= maxDelayMillisec);
            Assert.IsTrue(pollmeister.EmptyPollCallCount <= 12);
            Assert.IsTrue(pollmeister.PollCallCountWithPayload == 1);
        }

        [TestMethod]
        public void TestSmartPollingCallback()
        {
            DateTimeOffset threeSecondDelay = DateTimeOffset.Now.AddSeconds(3);

            const int maxDelayMillisec = 500;
            DateTimeOffset? message = null;
            BlockingObjectPoll<object> pollmeister;
            using (pollmeister = new BlockingObjectPoll<object>(() => PollCurrentTime(threeSecondDelay), maxDelayMillisec))
            {
                pollmeister.Subscribe(payload => message = payload == null ? (DateTimeOffset?)null : (DateTimeOffset)payload);
                Threading.Sleep(3100);
            }

            this.TestContext.WriteLine("Empty poll calls: {0:#,#0}, Calls with payload: {1:#,#0}", pollmeister.EmptyPollCallCount, pollmeister.PollCallCountWithPayload);

            Assert.IsTrue(message != null, "Message cannot be null.");
            int discrepMillisecBetweenHopedAndActual = (message.Value - threeSecondDelay).Milliseconds;
            Assert.IsTrue(discrepMillisecBetweenHopedAndActual <= maxDelayMillisec + 100, "After-wait delay is too long.");
            Assert.IsTrue(pollmeister.EmptyPollCallCount <= 12, "Number of poll hits is too low.");
            Assert.IsTrue(pollmeister.PollCallCountWithPayload >= 1, "There was no poll hit returning actual payload.");
        }
    }
}
// ReSharper restore JoinDeclarationAndInitializer
