using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Value.Framework.Core;
using Value.Framework.Aspectacular;

using Example.AdventureWorks2008ObjectContext_Dal;
using System.Diagnostics;

namespace Value.Framework.UnitTests.AspectacularTest
{
    [TestClass]
    public class LinqTests
    {
        const int customerIdWithManyAddresses = 29503;

        public static Aspect[] TestAspects
        {
            get { return AspectacularTest.TestAspects; }
        }

        /// <summary>
        /// A factory for getting AOP-augmented proxy to access AdventureWorksLT2008R2Entities instance members
        /// in allocate/invoke/dispose pattern.
        /// </summary>
        public static AllocateRunDisposeInterceptor<AdventureWorksLT2008R2Entities> AwDal
        {
            get { return AOP.GetAllocDisposeProxy<AdventureWorksLT2008R2Entities>(TestAspects); }
        }

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
            
            // Example 1: where AOP creates instance of AdventureWorksLT2008R2Entities, runs the DAL method, and disposes AdventureWorksLT2008R2Entities instance - all in one shot.
            addresses = AwDal.List(db => db.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));

            Assert.IsTrue(2 == addresses.Count);

            // Example 2: with simple AOP proxied call for existing instance of DbContext.
            using (var db = new AdventureWorksLT2008R2Entities())
            {
                addresses = db.GetProxy(TestAspects).List(inst => inst.QueryCustomerAddressesByCustomerID(customerIdWithManyAddresses));
            }

            Assert.IsTrue(2 == addresses.Count);
        }
    }
}
