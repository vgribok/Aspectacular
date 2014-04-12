using System;
using System.Text;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;

namespace Aspectacular.Test.CoreTests
{
    /// <summary>
    /// Summary description for EmailHelpersTest
    /// </summary>
    [TestClass]
    public class EmailHelpersTest
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
        public void TestEmailParser()
        {
            EmailAddress parsedEmail = " user@domain.com  ";
            string stringEmail = parsedEmail;
            Assert.AreEqual("user@domain.com", stringEmail);

            parsedEmail = "invalid email";
            Assert.IsFalse(parsedEmail.IsValid);
            stringEmail = parsedEmail;
            Assert.IsNull(stringEmail);

            parsedEmail = null as string;
            Assert.IsFalse(parsedEmail.IsValid);
            stringEmail = parsedEmail;
            Assert.IsNull(stringEmail);

            parsedEmail = null;
            Assert.IsFalse(parsedEmail.IsValid());

            parsedEmail = "first.last-third+filter@dom1.domain-ha.it";
            stringEmail = parsedEmail;
            Assert.AreEqual("first.last-third+filter@dom1.domain-ha.it", stringEmail);
            Assert.AreEqual("first.last-third@dom1.domain-ha.it", parsedEmail.AddressWithoutFilter);
            Assert.AreEqual("first.last-third+filter", parsedEmail[EmailAddressParts.UserBeforeAt]);
            Assert.AreEqual("first.last-third", parsedEmail[EmailAddressParts.UserBeforePlus]);
            Assert.AreEqual("filter", parsedEmail[EmailAddressParts.UserAfterPlusFilter]);
            Assert.AreEqual("dom1.domain-ha.it", parsedEmail[EmailAddressParts.Domain]);
            Assert.AreEqual("dom1.domain-ha", parsedEmail[EmailAddressParts.DomainMain]);
            Assert.AreEqual("it", parsedEmail[EmailAddressParts.DomainSuffix]);
        }
    }
}
