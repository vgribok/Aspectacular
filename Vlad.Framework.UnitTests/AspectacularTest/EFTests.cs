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
            addresses = AOP.GetAllocDisposeProxy<AdventureWorksLT2008R2Entities>(AspectacularTest.TestAspects)
                                .Invoke(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses).ToList());

            Assert.IsTrue(2 == addresses.Count);
        }
    }
}
