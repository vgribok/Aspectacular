#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using Example.AdventureWorks2008ObjectContext_Dal.DbCtx;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspectacular.Test
{
    [TestClass]
    public class EfExtensionsTest
    {
        public static Customer OrhpanCust
        {
            get { return new Customer {CustomerID = LinqTests.CustomerIdWithManyAddresses}; }
        }

        [TestMethod]
        public void TestDbCtxGetOrAttach()
        {
            //int id = LinqTests.customerIdWithManyAddresses;
            //var addresses = LinqTests.AwDal.List(db => db.QueryCustomerAddressesByCustomerID(id));
            //Assert.IsTrue(2 == addresses.Count);

            //LinqTests.AwDal.Invoke(db => db.ToStringEx(""));

            Customer cust = LinqTests.AwDal.Invoke(db => db.GetOrAttach(OrhpanCust, c => c.CustomerID));
            Assert.IsNull(cust.EmailAddress);


            Customer cust2;
            using(var dbc = new AdventureWorksLT2008R2Entities())
            {
                dbc.Configuration.LazyLoadingEnabled = false;

                cust = dbc.GetProxy(LinqTests.TestAspects).Single(db => db.QueryCustomerByID(LinqTests.CustomerIdWithManyAddresses));
                cust2 = dbc.GetProxy(LinqTests.TestAspects).Invoke(db => db.GetOrAttach(OrhpanCust));
            }
            Assert.IsNotNull(cust2.EmailAddress);
            Assert.IsTrue(cust.Equals(cust2));
        }
    }
}