#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Collections.Generic;
using System.Linq;
using Example.AdventureWorks2008ObjectContext_Dal.DbCtx;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test
{
    [TestClass]
    public class PerfTest
    {
        public TestContext TestContext { get; set; }

        public const int MillisecToRun = 2*1000;
        private const int customerIdWithManyAddresses = 29503;

        public static IEnumerable<Aspect> TestAspects
        {
            get { return AspectacularTest.TestAspects; }
        }

        [TestMethod]
        public void CallConstPerfCounter()
        {
            const int baseLineConstParmRunsPerSec = 4000;

            var dal = new SomeTestClass();

            long runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () => dal.GetProxy().Invoke(ctx => ctx.DoNothing(123, "bogus", false, 1m, null)));
            this.TestContext.WriteLine("DoNothing() INSTANCE PROXIED SQUENTIAL CONSTANTPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineConstParmRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineConstParmRunsPerSec);
        }

        [TestMethod]
        public void CallConstStaticPerfCounter()
        {
            const int baseLineMultiThreadConstStaticParmRunsPerSec = 23000; // 31500; // 33500;
            const int baseLineSingleThreadConstStaticParmRunsPerSec = 8500; // 9000

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            this.TestContext.WriteLine("DoNothingStatic() STATIC PROXIED PARALLEL LOCALVARS CONSTANTPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineMultiThreadConstStaticParmRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineMultiThreadConstStaticParmRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(123, "bogus", false, 1m, null)));
            this.TestContext.WriteLine("DoNothingStatic() STATIC PROXIED SEQUENTIAL CONSTANTPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineSingleThreadConstStaticParmRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineSingleThreadConstStaticParmRunsPerSec);
        }

        [TestMethod]
        public void CallPerfCounter()
        {
            const int baseLineSingleThreadRunsPerSec = 2700;
            const int baseLineMultiThreadRunsPerSec = 9500; // 10000;

            var dal = new SomeTestClass();
            long runsPerSec;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = {1, 2, 3, 4, 5};

            runsPerSec = RunCounter.SpinParallelPerSec(MillisecToRun, () => dal.GetProxy().Invoke(ctx => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            this.TestContext.WriteLine("Worst case scenario: DoNothing() INSTANCE PROXIED PARALLEL VARPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineMultiThreadRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineMultiThreadRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () => dal.GetProxy().Invoke(ctx => ctx.DoNothing(parmInt, parmStr, parmBool, parmDec, arr)));
            this.TestContext.WriteLine("DoNothing() INSTANCE PROXIED SEQUENTIAL VARPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineSingleThreadRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineSingleThreadRunsPerSec);
        }

        [TestMethod]
        public void CallPerfStaticCounter()
        {
            const int baseLineParallelRunsPerSec = 9500; // 10000;
            const int baseLineRunsPerSec = 3300; // 3500;

            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = {1, 2, 3, 4, 5};

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            this.TestContext.WriteLine("DoNothingStatic() STATIC PROXIED PARALLEL VRPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineParallelRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineParallelRunsPerSec);

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            this.TestContext.WriteLine("DoNothingStatic() STATIC PROXIED SEQUENTIAL VARPARAMS got {0} cps, with expected {1} cps.", runsPerSec, baseLineRunsPerSec);
            //Assert.IsTrue(runsPerSec >= baseLineRunsPerSec);
        }

        [TestMethod]
        public void CallPerfStaticCounterProfile()
        {
            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = {1, 2, 3, 4, 5};

            long runsPerSec;

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));
            this.TestContext.WriteLine("SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr) non-parallel perf test result: {0} calls/second.", runsPerSec);
        }

        [TestMethod]
        public void CallPerfParallelStaticCounterProfile()
        {
            int parmInt = 123;
            string parmStr = "bogus";
            bool parmBool = false;
            decimal parmDec = 1.0m;
            int[] arr = {1, 2, 3, 4, 5};

            long runsPerSec;

            runsPerSec = RunCounter.SpinParallelPerSec(MillisecToRun, () => AOP.Invoke(() => SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr)));

            this.TestContext.WriteLine("SomeTestClass.DoNothingStatic(parmInt, parmStr, parmBool, parmDec, arr) parallel perf test result: {0} calls/second.", runsPerSec);
        }


        [TestMethod]
        public void CallPerfDbContextDirect()
        {
            long runsPerSec;

            using(var db = new AdventureWorksLT2008R2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;

                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () =>
                    db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList()
                    );
            }
            this.TestContext.WriteLine("db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses) direct sequential base line test result: {0} calls/second.", runsPerSec);
        }

        [TestMethod]
        public void CallPerfDbContextAugmented()
        {
            long runsPerSec;

            using(var db = new AdventureWorksLT2008R2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;

                runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () =>
                    db.GetProxy().List(inst => inst.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses))
                    );
            }
            this.TestContext.WriteLine("db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses) augmented sequential base line test result: {0} calls/second.", runsPerSec);
        }


        [TestMethod]
        public void CallPerfDbContextDirectParallel()
        {
            long runsPerSec;

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () =>
            {
                using(var db = new AdventureWorksLT2008R2Entities())
                {
                    db.Configuration.LazyLoadingEnabled = false;

// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                    db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList();
                }
            });

            this.TestContext.WriteLine("db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses) direct parallel alloc/call/disp base line test result: {0} calls/second.", runsPerSec);
        }

        [TestMethod]
        public void CallPerfDbContextAugmentedParallel()
        {
            long runsPerSec;

            runsPerSec = RunCounter.SpinPerSec(MillisecToRun, () =>
                EfAOP.GetDbProxy<AdventureWorksLT2008R2Entities>()
                    .List(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses))
                );

            this.TestContext.WriteLine("db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses) augmented parallel alloc/invoke/disp base line test result: {0} calls/second.", runsPerSec);
        }
    }
}