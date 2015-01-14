#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
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

        public IQueryable<Address> SearchAddress(string addressString)
        {
            return this.Addresses.Where(x => x.AddressLine1.Contains(addressString));
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

        public IQueryable<Customer> QueryAllCustomers()
        {
            return this.Customers;
        }

        public IQueryable<StateCity> QueryAllStateCities()
        {
            var q = from a in this.Addresses
                select new StateCity
                {
                    City = a.City,
                    StateProvince = a.StateProvince
                };

            return q.Distinct();
        }

        public IQueryable<StateZipCode> QueryAllStateZipCodes()
        {
            var q = from a in this.Addresses
                    select new StateZipCode
                    {
                        PostalCode = a.PostalCode,
                        StateProvince = a.StateProvince
                    };

            return q.Distinct();
        }

        public IQueryable<StateCityZip> QueryAllStateCityZips()
        {
            IQueryable<StateCity> cities = this.QueryAllStateCities();
            IQueryable<StateZipCode> zips = this.QueryAllStateZipCodes();

            IQueryable<StateCityZip> stateCityZips = cities.FullOuterJoin(zips, s => s.StateProvince, z => z.StateProvince, 
                (s, z) => new StateCityZip
                    {
                        StateProvince = s.StateProvince,
                        PostalCode = z.PostalCode,
                        City = s.City
                    }
                );

            return stateCityZips;
        }

        public List<StateCityZip> GetAllStateCityZips()
        {
            IEnumerable<StateCity> cities = this.QueryAllStateCities().ToList();
            IEnumerable<StateZipCode> zips = this.QueryAllStateZipCodes().ToList();

            IEnumerable<StateCityZip> stateCityZips = cities.FullOuterJoin(zips, s => s.StateProvince, z => z.StateProvince,
                (s, z) => new StateCityZip
                    {
                        StateProvince = s.StateProvince,
                        PostalCode = z.PostalCode,
                        City = s.City
                    }
                );

            return stateCityZips.ToList();
        }
    }

    public class StateCity
    {
        public string City { get; set; }
        public string StateProvince { get; set; }
    }

    public class StateZipCode
    {
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }
    }

    public class StateCityZip : IEquatable<StateCityZip>
    {
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }

        bool IEquatable<StateCityZip>.Equals(StateCityZip other)
        {
            if(other == null)
                return false;

            if(other == this)
                return true;

            bool same = this.PostalCode == other.PostalCode && this.City == other.City && this.StateProvince == other.StateProvince;
            return same;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = GetStringHash(this.City) ^ GetStringHash(this.StateProvince) ^ GetStringHash(this.PostalCode);
            return hash;
        }

        private static int GetStringHash(string str)
        {
            return str == null ? 0 : str.GetHashCode();
        }
    }
}