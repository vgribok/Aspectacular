using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Diagnostics;
using System.Collections;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Aspectacular;
using Example.AdventureWorks2008ObjectContext_Dal.DbCtx;
//using ObjCtx = Example.AdventureWorks2008ObjectContext_Dal.ObjCtx;

namespace Aspectacular.Test
{
    [TestClass]
    public class LinqTests
    {
        public TestContext TestContext { get; set; }

        public const int customerIdWithManyAddresses = 29503;

        public static IEnumerable<Aspect> TestAspects
        {
            get { return AspectacularTest.TestAspects; }
        }

        /// <summary>
        /// A factory for getting AOP-augmented proxy to access AdventureWorksLT2008R2Entities instance members
        /// in allocate/invoke/dispose pattern.
        /// </summary>
        public static DbContextSingleCallProxy<AdventureWorksLT2008R2Entities> AwDal
        {
            get { return EfAOP.GetDbProxy<AdventureWorksLT2008R2Entities>(TestAspects, lazyLoadingEnabled: false); }
        }

        //public static ObjectContextSingleCallProxy<ObjCtx.AdventureWorksLT2008R2EntitiesObjCtx> AwDalOcx
        //{
        //    get { return EfAOP.GetOcProxy<ObjCtx.AdventureWorksLT2008R2EntitiesObjCtx>(TestAspects, lazyLoadingEnabled: false); }
        //}

        [TestMethod]
        public void LinqTestOne()
        {
            IList<Address> addresses;

            // Without AOP
            using (var db = new AdventureWorksLT2008R2Entities())
            {
                addresses = db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList();
            }

            Assert.IsTrue(2 == addresses.Count);

            // With LINQ-friendly AOP shortcut
            
            // Example 1: where AOP creates instance of AdventureWorksLT2008R2Entities, runs the DAL method, 
            // and disposes AdventureWorksLT2008R2Entities instance - all in one shot.
            addresses = AwDal.List(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));

            Assert.IsTrue(2 == addresses.Count);

            // Example 2: with simple AOP proxied call for existing instance of DbContext.
            using (var db = new AdventureWorksLT2008R2Entities())
            {
                addresses = db.GetDbProxy(TestAspects).List(inst => inst.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));
            }

            Assert.IsTrue(2 == addresses.Count);
        }

        //[TestMethod]
        //public void TestObjectContextEf()
        //{
        //    using (var db = new ObjCtx.AdventureWorksLT2008R2EntitiesObjCtx())
        //    {
        //        var addresses = db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList();
        //        Assert.IsTrue(2 == addresses.Count);
        //    }

        //    var addresses2 = AwDalOcx.List(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));
        //    Assert.IsTrue(2 == addresses2.Count);
        //}

        internal static IList<Address> GetQueryCustomerAddressesByCustomerID()
        {
            var addresses = AwDal.List(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));
            return addresses;
        }

        [TestMethod]
        public void TestAnonymousQuery()
        {
            List<object> countryStateBityRecords = AwDal.List(db => db.QueryUserCoutryStateCity(customerIdWithManyAddresses));

            foreach (var record in countryStateBityRecords)
                this.TestContext.WriteLine("{0}", record);
        }

        [TestMethod]
        public void TestExecuteCommand()
        {
            Customer mrAndrewCencini;
            const string originalEmail = "andrew2@adventure-works.com";
            const string anotherEmail = "VH.andrew2@adventure-works.com";
            string replacementEmail;

            using (var dbc = new AdventureWorksLT2008R2Entities())
            {
                mrAndrewCencini = dbc.GetDbProxy(TestAspects).Single(db => db.QueryCustomerByID(163));
                
                replacementEmail = mrAndrewCencini.EmailAddress == originalEmail ? anotherEmail : originalEmail;
                mrAndrewCencini.EmailAddress = replacementEmail;

                int recordsTouched = dbc.GetDbProxy(TestAspects).ExecuteCommand(db => db.ToStringEx(""));
                Assert.AreEqual(1, recordsTouched);
                mrAndrewCencini = dbc.GetDbProxy(TestAspects).Single(db => db.QueryCustomerByID(163));
            }

            Assert.AreEqual(mrAndrewCencini.EmailAddress, replacementEmail);

            //Customer mrAndrewCencini = new Customer { CustomerID = 163 };
            //int retVal = AwDal.ExecuteCommand(db => db.DeleteEntity(mrAndrewCencini));
            //Assert.AreEqual(1, retVal);
        }

        [TestMethod]
        public void TestOrderByPropertyName()
        {
            string[] strs = { "Very long", "Short" };

            Assert.AreEqual(strs[0].Length, strs[0].GetPropertyValue<int>("Length"));

            string[] sorted = strs.OrderByProperty("Length").ToArray();
            Assert.AreEqual(strs[0], sorted[1]);

            sorted = strs.OrderByProperty(null).ToArray();
            Assert.AreEqual(strs[0], sorted[0]);
        }
    }
}
