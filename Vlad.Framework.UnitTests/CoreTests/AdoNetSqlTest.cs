using System;
using System.Text;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Example.AdventureWorks2008ObjectContext_Dal.Ado.net.AwDataSetTableAdapters;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for AdoNetSqlTest
    /// </summary>
    [TestClass]
    public class AdoNetSqlTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        protected static AllocateRunDisposeProxy<ProductCategoryTableAdapter> TblAdapterProxy
        {
            get { return new AllocateRunDisposeProxy<ProductCategoryTableAdapter>(AspectacularTest.TestAspects); }
        }

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
        public void ProductCategoryTableAdapterTest()
        {
            var data = TblAdapterProxy.Invoke(db => db.GetCategoriesByNameLike("road"));
            Assert.AreEqual(2, data.Rows.Count);
        }
    }
}
