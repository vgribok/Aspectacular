using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for XmlSerializationTest
    /// </summary>
    [TestClass]
    public class XmlSerializationTest
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

        public class ObjectTree
        {
            [XmlElement]
            public string Foo { get; set; }

            [XmlAttribute]
            public int Bar { get; set; }

            [XmlElement]
            public List<object> Nested { get; set; }

            [XmlAnyElement]
            public XmlElement AnyXml { get; set; }
        }

        [TestMethod]
        public void TestXmlDocumentSeiralization()
        {
            var objTree = new ObjectTree
            {
                Foo = "Foo1",
                Bar = 1,
                Nested = new List<object>()
            };

            objTree.Nested.AddRange(new[]
                {
                    new ObjectTree{ Foo = "Foo2", Bar = 2},
                    new ObjectTree{ Foo = "Foo3", Bar = 3},
                });

            XmlDocument xmlDoc = objTree.ToXmlDocument();
            string xml = xmlDoc.ToXml();
            Assert.IsNotNull(xml);

            XmlNode parentElem = xmlDoc.DocumentElement;
            objTree.SerializeInto(parentElem);

            xml = xmlDoc.ToXml();
            Assert.IsNotNull(xml);

            xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            ObjectTree newObjectTree = xmlDoc.Deserialize<ObjectTree>();
            Assert.IsNotNull(newObjectTree);

            ObjectTree nested = newObjectTree.AnyXml.Deserialize<ObjectTree>();
            Assert.AreEqual(1, nested.Bar);
            Assert.AreEqual(objTree.Nested.Count, nested.Nested.Count);
        }
    }
}
