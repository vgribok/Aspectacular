using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Aspectacular.Test
{
    /// <summary>
    /// Summary description for RetryAspectsTest
    /// </summary>
    [TestClass]
    public class RetryAspectsTest
    {
        private DateTime? testStart;
        private uint iteration = 0;

        public RetryAspectsTest()
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
        public void SuccessAfterRetryTest()
        {
            this.iteration = 0;
            Aspect retryAspect = new RetryCountAspect(retryCount: 3);
            DateTime dt = (DateTime)this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect)).Invoke(test => test.SimulateFailureNTimes(2, true));

            this.testStart = DateTime.UtcNow;
            retryAspect = new RetryTimeAspect(keepTryingForMilliseconds: 300, millisecDelayBetweenRetries: 100);
            dt = (DateTime)this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect)).Invoke(test => test.SimulateFailureFor(200, true));

            this.iteration = 0;
            retryAspect = new RetryCountAspect(3, 50, proxy => proxy.ReturnedValue == null);
            dt = (DateTime)this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect)).Invoke(test => test.SimulateFailureNTimes(2, false));

            this.testStart = DateTime.UtcNow;
            retryAspect = new RetryTimeAspect(keepTryingForMilliseconds: 300, millisecDelayBetweenRetries: 100, optionalRetryDecider: proxy => proxy.ReturnedValue == null);
            dt = (DateTime)this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect)).Invoke(test => test.SimulateFailureFor(200, false));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PersistentFailureTest()
        {
            this.iteration = 0;
            Aspect retryAspect = new RetryCountAspect(retryCount: 3);
            this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect)).Invoke(test => test.SimulateFailureNTimes(3, true));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void AbortedRetryTest()
        {
            this.iteration = 0;
            Aspect retryAspect = new RetryCountAspect(retryCount: 3, millisecDelayBetweenRetries: 100);
            
            // A thread simulating asynchronous application exit.
            Task asyncAbortGenerator = Task.Run(() => 
            {
                Thread.Sleep(100);
                Threading.ApplicationExiting.Set();
            });

            var proxy = this.GetProxy(AspectacularTest.MoreTestAspects(retryAspect));

            try
            {
                proxy.Invoke(test => test.SimulateFailureNTimes(3, true));
            }
            finally
            {
                Threading.ApplicationExiting.Reset();

                bool aborted = proxy.callLog.Exists(l => l.Key == "Retry Aborted");
                Assert.IsTrue(aborted);

                asyncAbortGenerator.Wait();
            }
        }

        #region Failure simulation methods

        /// <summary>
        /// Will be failing for a given period of time since test start.
        /// </summary>
        /// <param name="millisec"></param>
        /// <returns></returns>
        private object SimulateFailureFor(uint millisec, bool throwExceptionOnFailure = true)
        {
            DateTime now = DateTime.UtcNow;

            uint elpased = (uint)(now - this.testStart.Value).TotalMilliseconds;

            if (elpased <= millisec)
            {
                if (throwExceptionOnFailure)
                    throw new Exception("Simulated time failure");
                else
                    return null;
            }

            return now;
        }

        /// <summary>
        /// Will be failing a few times it's called.
        /// </summary>
        /// <param name="timesToFail"></param>
        /// <param name="throwExceptionOnFailure"></param>
        /// <returns></returns>
        private object SimulateFailureNTimes(uint timesToFail, bool throwExceptionOnFailure = true)
        {
            this.iteration++;

            if (this.iteration <= timesToFail)
            {
                if (throwExceptionOnFailure)
                    throw new Exception("Simulated count failure");
                else
                    return null;
            }

            return DateTime.UtcNow;
        }

        #endregion Failure simulation methods
    }
}
