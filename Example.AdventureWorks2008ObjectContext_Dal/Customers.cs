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

        //public void Bogus(int customerID)
        //{
        //    this.ToString();
        //}
    }
}