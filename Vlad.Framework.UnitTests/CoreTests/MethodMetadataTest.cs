using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for MethodMetadataTest
    /// </summary>
    [TestClass]
    public class MethodMetadataTest
    {
        private TestContext testContextInstance { get; set; }

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
        public void TestValueFormatting()
        {
            List<Tuple<int, string>> whatevs = new List<Tuple<int, string>>();
            whatevs.Add(new Tuple<int, string>(1, "One"));
            whatevs.Add(new Tuple<int, string>(2, "Two"));

            IEnumerable<Tuple<int, string>> bad = whatevs;

            //Type type = typeof(IEnumerable<Tuple<int, string>>);
            Type type = bad.GetType();

            string valuString = InterceptedMethodParamMetadata.FormatParamValue(type, whatevs, trueUi_FalseInternal: false);
            Assert.IsTrue(valuString.Contains("HASH"));
        }
    }
}
