using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Aspectacular;
using Value.Framework.Data.EntityFramework;

namespace Example.AdventureWorks2008ObjectContext_Dal
{
    public partial class Customer : IDbEntityKey
    {
        public object DbContextEntityKey
        {
            get { return this.CustomerID; }
        }
    }

    public partial class AdventureWorksLT2008R2Entities
    {
        public IQueryable<Customer> QueryCustomerByID(int customerID)
        {
            this.LogInformationData("customerID", customerID);

            var q = from cust in this.Customers
                    where cust.CustomerID == customerID
                    select cust;

            return q;
        }

        public void Bogus(int customerID)
        {
            this.ToString();
        }
    }
}
