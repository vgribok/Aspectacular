using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aspectacular;
using Example.AdventureWorks2008ObjectContext_Dal.DbCtx;

namespace Aspectacular.Test
{
    [TestClass]
    public class EfExtensionsTest
    {
        public static Customer OrhpanCust
        {
            get { return new Customer() { CustomerID = LinqTests.customerIdWithManyAddresses }; }
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
            using (var dbc = new AdventureWorksLT2008R2Entities())
            {
                cust = dbc.GetProxy(LinqTests.TestAspects).Single(db => db.QueryCustomerByID(LinqTests.customerIdWithManyAddresses));
                cust2 = dbc.GetProxy(LinqTests.TestAspects).Invoke(db => db.GetOrAttach(OrhpanCust));
            }
            Assert.IsNotNull(cust2.EmailAddress);
            Assert.IsTrue(cust.Equals(cust2));
        }
    }
}
