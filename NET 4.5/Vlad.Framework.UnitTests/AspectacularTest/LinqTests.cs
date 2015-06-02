#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Collections.Generic;
using System.Linq;
using Example.AdventureWorks2008ObjectContext_Dal.DbCtx;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//using ObjCtx = Example.AdventureWorks2008ObjectContext_Dal.ObjCtx;

namespace Aspectacular.Test
{
    [TestClass]
    public class LinqTests
    {
        #region Initialization

        public TestContext TestContext { get; set; }

        public const int CustomerIdWithManyAddresses = 29503;

        public static IEnumerable<Aspect> TestAspects
        {
            get { return AspectacularTest.TestAspects; }
        }

        #endregion Initialization

        /// <summary>
        ///     A factory for getting AOP-augmented proxy to access AdventureWorksLT2008R2Entities instance members
        ///     in allocate/invoke/dispose pattern.
        /// </summary>
        public static DbContextSingleCallProxy<AdventureWorksLT2008R2Entities> AwDal
        {
            get { return EfAOP.GetDbProxy<AdventureWorksLT2008R2Entities>(TestAspects, false); }
        }

        //public static ObjectContextSingleCallProxy<ObjCtx.AdventureWorksLT2008R2EntitiesObjCtx> AwDalOcx
        //{
        //    get { return EfAOP.GetOcProxy<ObjCtx.AdventureWorksLT2008R2EntitiesObjCtx>(TestAspects, lazyLoadingEnabled: false); }
        //}

        [TestMethod]
        public void LinqTestOne()
        {
            IList<Address> addresses;

            // Regular EF call without AOP:
            using(var db = new AdventureWorksLT2008R2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                addresses = db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses).ToList();
            }
            Assert.AreEqual(2, addresses.Count);

            // Now same with LINQ-friendly AOP shortcuts:

            // Example 1: where AOP creates instance of AdventureWorksLT2008R2Entities, runs the DAL method, 
            // and disposes AdventureWorksLT2008R2Entities instance - all in one shot.
            addresses = AwDal.List(db => db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses));
            Assert.AreEqual(2, addresses.Count);

            // Example 2: with simple AOP proxied call for existing instance of DbContext.
            using(var db = new AdventureWorksLT2008R2Entities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                addresses = db.GetDbProxy(TestAspects).List(dbx => dbx.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses));
            }
            Assert.AreEqual(2, addresses.Count);

            var address = AwDal.Single(db => db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses));
            Assert.IsNotNull(address);

            long adrressCount = AwDal.Count(db => db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses));
            Assert.AreEqual(2, adrressCount);

            adrressCount = AwDal.Count(db => db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses), new QueryModifiers().AddSortCriteria("AddressID").AddPaging(0, 1));
            Assert.AreEqual(1, adrressCount);
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

        internal static IList<Address> GetQueryCustomerAddressesByCustomerId()
        {
            IList<Address> addresses = AwDal.List(db => db.QueryCustomerAddressesByCustomerID(CustomerIdWithManyAddresses));
            return addresses;
        }

        [TestMethod]
        public void TestAnonymousQuery()
        {
            List<object> countryStateBityRecords = AwDal.List(db => db.QueryUserCoutryStateCity(CustomerIdWithManyAddresses));

            foreach(object record in countryStateBityRecords)
                this.TestContext.WriteLine("{0}", record);
        }

        [TestMethod]
        public void TestExecuteCommand()
        {
            Customer mrAndrewCencini;
            const string originalEmail = "andrew2@adventure-works.com";
            const string anotherEmail = "VH.andrew2@adventure-works.com";
            string replacementEmail;

            using(var dbc = new AdventureWorksLT2008R2Entities())
            {
                dbc.Configuration.LazyLoadingEnabled = false;

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
            string[] strs = {"Very long", "Short"};

            Assert.AreEqual(strs[0].Length, strs[0].GetPropertyValue<int>("Length"));

            string[] sorted = strs.OrderByProperty("Length").ToArray();
            Assert.AreEqual(strs[0], sorted[1]);

            sorted = strs.OrderByProperty(null).ToArray();
            Assert.AreEqual(strs[0], sorted[0]);
        }

        [TestMethod]
        public void TestQueryModifiers()
        {
            long customerCount = AwDal.Count(db => db.QueryAllCustomers());
            Assert.AreEqual(847, customerCount);

            QueryModifiers mods = new QueryModifiers();
            customerCount = AwDal.Count(db => db.QueryAllCustomers(), mods);
            Assert.AreEqual(847, customerCount);

            mods.AddSortCriteria("CustomerID");
            var customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            var customer = customers.First();
            Assert.AreEqual("Orlando", customer.FirstName);
            Assert.AreEqual("Gee", customer.LastName);
            
            mods.AddPaging(0, 5);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.AreEqual(5, customers.Count);
            customer = customers.First();
            Assert.AreEqual("Orlando", customer.FirstName);
            Assert.AreEqual("Gee", customer.LastName);

            mods.Paging = null;
            mods.AddFilter("FirstName", DynamicFilterOperator.Equal, "John");
            mods.AddFilter("NameStyle", DynamicFilterOperator.Equal, false);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.AreEqual(20, customers.Count);
            customer = customers[2];
            Assert.AreEqual(309, customer.CustomerID);
            Assert.AreEqual("John", customer.FirstName);
            Assert.AreEqual("Arthur", customer.LastName);

            mods.AddPaging(pageIndex: 2, pageSize: 3);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.AreEqual(3, customers.Count);
            customer = customers[0];
            Assert.AreEqual(471, customer.CustomerID);
            Assert.AreEqual("John", customer.FirstName);
            Assert.AreEqual("Ford", customer.LastName);

            mods = new QueryModifiers();
            mods.AddSortCriteria("FirstName").AddSortCriteria("LastName", QueryModifiers.SortOrder.Descending);
            mods.AddPaging(pageIndex: 0, pageSize: 3);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.AreEqual(3, customers.Count);
            customer = customers[1];
            Assert.AreEqual(29943, customer.CustomerID);
            Assert.AreEqual("A.", customer.FirstName);
            Assert.AreEqual("Leonetti", customer.LastName);

            mods.AddFilter("LastName", DynamicFilterOperator.StringStartsWith, "Ve");
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.IsTrue(customers.All(c => c.LastName.StartsWith("Ve")));

            //int hash1 = mods.GetHashCode();
            string mods1 = mods.ToString();
            mods.Filters.Clear();
            mods.AddFilter("LastName", DynamicFilterOperator.StringContains, "er");
            //int hash2 = mods.GetHashCode();
            string mods2 = mods.ToString();
            Assert.AreNotEqual(mods1, mods2);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.IsTrue(customers.All(c => c.LastName.Contains("er")));
            customer = customers[2];
            Assert.AreEqual(29583, customer.CustomerID);
            Assert.AreEqual("Alan", customer.FirstName);
            Assert.AreEqual("Brewer", customer.LastName);

            mods = new QueryModifiers().AddPaging(pageIndex: 1, pageSize: 5).AddSortCriteria("LastName").AddFilter("CustomerID", DynamicFilterOperator.LessThan, 10);
            customers = AwDal.List(db => db.QueryAllCustomers(), mods);
            Assert.IsTrue(customers.All(c => c.CustomerID < 10));
            Assert.AreEqual(2, customers.Count);
            customer = customers[0];
            Assert.AreEqual(5, customer.CustomerID);
            Assert.AreEqual("Lucy", customer.FirstName);
            Assert.AreEqual("Harrington", customer.LastName);
        }

        [TestMethod]
        public void TestFullOuterJoin()
        {
            var items1 = AwDal.Invoke(db => db.GetAllStateCityZips());
            this.TestContext.WriteLine("In-memory OuterJoin count: {0}", items1.Count);

            var items2 = AwDal.List(db => db.QueryAllStateCityZips());
            this.TestContext.WriteLine("OuterJoin count: {0}", items2.Count);

            Assert.AreEqual(items1.Count, items2.Count);
        }

        [TestMethod]
        public void TestExistsExtensionMethod()
        {
            long barCount = AwDal.Count(db => db.SearchAddress("bar"));
            Assert.IsTrue(barCount == 1);

            bool addressExists = AwDal.Exists(db => db.SearchAddress("bar"));
            Assert.IsTrue(addressExists);
        }
    }
}