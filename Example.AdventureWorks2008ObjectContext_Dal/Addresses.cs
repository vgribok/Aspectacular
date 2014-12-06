#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Linq;
using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.ObjCtx
{
    //public partial class AdventureWorksLT2008R2EntitiesObjCtx
    //{
    //    public IQueryable<Address> QueryCustomerAddressesByCustomerID(int customerID)
    //    {
    //        this.LogInformationData("customerID", customerID);

    //        var q = from caddr in this.CustomerAddresses
    //                join addr in this.Addresses on caddr.AddressID equals addr.AddressID
    //                where caddr.CustomerID == customerID
    //                select addr;

    //        return q;
    //    }
    //}
}

namespace Example.AdventureWorks2008ObjectContext_Dal.DbCtx
{
    public partial class AdventureWorksLT2008R2Entities
    {
        /// <summary>
        ///     Returns single record query of Address by its ID.
        /// </summary>
        /// <param name="addressID"></param>
        /// <returns></returns>
        public IQueryable<Address> QueryAddressByID(int addressID)
        {
            this.LogInformationData("addressID", addressID);

            IQueryable<Address> q = from address in this.Addresses
                where address.AddressID == addressID
                select address;

            return q;
        }

        public IQueryable<Address> QueryCustomerAddressesByCustomerID(int customerID)
        {
            this.LogInformationData("customerID", customerID);

            IQueryable<Address> q = from caddr in this.CustomerAddresses
                join addr in this.Addresses on caddr.AddressID equals addr.AddressID
                where caddr.CustomerID == customerID
                select addr;

            return q;
        }

        public IQueryable<Address> QueryCustomerAddresses(IQueryable<Customer> customer)
        {
            IQueryable<Address> q = from caddr in customer.SelectMany(cust => cust.CustomerAddresses)
                join addr in this.Addresses on caddr.AddressID equals addr.AddressID
                select addr;

            return q;
        }

        public IQueryable QueryUserCoutryStateCity(int customerID)
        {
            this.LogInformationData("customerID", customerID);

            var q = from custAddr in this.QueryCustomerAddressesByCustomerID(customerID)
                select new
                {
                    custAddr.City,
                    custAddr.StateProvince,
                    custAddr.CountryRegion
                };

            return q.Distinct();
        }

        [InvariantReturn(false)]
        public IQueryable<Customer> QueryAllCustomers()
        {
            return this.Customers;
        }
    }
}