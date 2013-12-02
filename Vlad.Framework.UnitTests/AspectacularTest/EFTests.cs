using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Core;
using Value.Framework.Aspectacular;

using Example.AdventureWorks2008ObjectContext_Dal;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class EFTests
    {
        const int customerIdWithManyAddresses = 29503;

        public static Aspect[] TestAspects
        {
            get { return AspectacularTest.TestAspects; }
        }

        [TestMethod]
        public void EfTestOne()
        {
            List<Address> addresses;

            // Without AOP
            using (var db = new AdventureWorksLT2008R2Entities())
            {
                addresses = db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList();
            }

            Assert.IsTrue(2 == addresses.Count);

            // With regular AOP (w/o EF-specific AOP support)
            
            // Example 1: where AOP creates instance of AdventureWorksLT2008R2Entities, runs the DAL method, and disposes AdventureWorksLT2008R2Entities instance - all in one shot.
            addresses = AOP.GetAllocDisposeProxy<AdventureWorksLT2008R2Entities>(TestAspects)
                                .Invoke(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList());

            Assert.IsTrue(2 == addresses.Count);

            // Example 2: with simple AOP proxied call for existing instance of DbContext.
            using (var db = new AdventureWorksLT2008R2Entities())
            {
                addresses = db.GetProxy(TestAspects).Invoke(inst => inst.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList());
            }

            Assert.IsTrue(2 == addresses.Count);
        }
    }
}
