#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Linq;
using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.DbCtx
{
    public partial class Customer : IDbEntityKey
    {
        public object DbContextEntityKey
        {
            get { return this.CustomerID; }
        }
    }

    public class CAddress
    {
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public int AddressID { get; set; }
    }

    public partial class AdventureWorksLT2008R2Entities
    {
        public IQueryable<Customer> QueryCustomerByID(int customerID)
        {
            this.LogInformationData("customerID", customerID);

            IQueryable<Customer> q = from cust in this.Customers
                where cust.CustomerID == customerID
                select cust;

            return q;
        }

        public IQueryable<CAddress> DenormAddresses()
        {
            return this.Customers.FullOuterJoin(this.CustomerAddresses,
                c => c.CustomerID, ca => ca.CustomerID,
                (c, ca) => 
                    new CAddress
                    {
                        AddressID = ca.AddressID,
                        CustomerID = c.CustomerID,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                    }
                );
        }
    }
}