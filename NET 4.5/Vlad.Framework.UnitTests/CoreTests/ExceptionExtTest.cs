#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    ///     Summary description for ExceptionExtTest
    /// </summary>
    [TestClass]
    public class ExceptionExtTest
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
        public void ExceptionStringTest()
        {
            string actual;

            actual = ((Exception)null).ConsolidatedMessage();
            Assert.IsNull(actual);

            try
            {
                throw new NotImplementedException("Inner exception");
            }
            catch(Exception exInner)
            {
                actual = exInner.ConsolidatedStack();
                Assert.IsTrue(actual.Length > 0);
                Assert.IsFalse(actual.Contains(ExceptionExtensions.defaultItemSeparator));

                try
                {
                    throw new Exception("Outer", exInner);
                }
                catch(Exception outer)
                {
                    actual = outer.ConsolidatedStack();
                    Assert.IsTrue(actual.Contains(ExceptionExtensions.defaultItemSeparator));

                    actual = outer.ConsolidatedInfo();
                    Assert.IsTrue(actual.Contains(ExceptionExtensions.defaultItemSeparator));

                    const bool innerFirst = true;
                    actual = outer.Consolidate("", innerFirst, ex => ex.ToSerializable().ToXml());
                    this.TestContext.WriteLine(actual);

                    actual = outer.Consolidate("", innerFirst, ex => ex.ToSerializable().ToJsonString());
                    this.TestContext.WriteLine("{0}", actual);
                }
            }
        }
    }
}