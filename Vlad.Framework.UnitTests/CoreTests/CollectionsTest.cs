using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for CollectionsTest
    /// </summary>
    [TestClass]
    public class CollectionsTest
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
        public void SetCompareTest()
        {
            int[] currentSet = { 1, 2, 3, 4 };
            int[] newSet = { 3, 4, 5, 6 };
            int[] nullSet = null;

            SetComparisonResult<int> result = currentSet.CompareSets(newSet);
            int[] expectedAdd = { 5, 6 };
            int[] expectedDelete = { 1, 2 };
            Assert.IsTrue(expectedAdd.HaveSameElements(result.ToBeAdded));
            Assert.IsTrue(expectedDelete.HaveSameElements(result.ToBeDeleted));

            result = nullSet.CompareSets(newSet);
            Assert.IsTrue(newSet.HaveSameElements(result.ToBeAdded));
            Assert.IsTrue(nullSet.HaveSameElements(result.ToBeDeleted));

            result = currentSet.CompareSets(nullSet);
            Assert.IsTrue(nullSet.HaveSameElements(result.ToBeAdded));
            Assert.IsTrue(currentSet.HaveSameElements(result.ToBeDeleted));
        }
    }
}
