﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example.AdventureWorks2008ObjectContext_Dal
{
    public partial class AdventureWorksLT2008R2Entities
    {
        /// <summary>
        /// Returns single record query of Address by its ID.
        /// </summary>
        /// <param name="addressID"></param>
        /// <returns></returns>
        public IQueryable<Address> QueryAddressByID(int addressID)
        {
            var q = from address in this.Addresses
                    where address.AddressID == addressID
                    select address;

            return q;
        }

        public IQueryable<Address> QueryCustomerAddressesByCustomerID(int customerID)
        {
            var q = from caddr in this.CustomerAddresses
                    join addr in this.Addresses on caddr.AddressID equals addr.AddressID
                    where caddr.CustomerID == customerID
                    select addr;

            return q;
        }

        public IQueryable<Address> QueryCustomerAddresses(IQueryable<Customer> customer)
        {
            var q = from caddr in customer.SelectMany(cust => cust.CustomerAddresses)
                    join addr in this.Addresses on caddr.AddressID equals addr.AddressID
                    select addr;

            return q;
        }
    }
}