#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Data.SqlClient;
using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.Ado.net
{
    public partial class AwDataSet
    {
    }
}

namespace Example.AdventureWorks2008ObjectContext_Dal.Ado.net.AwDataSetTableAdapters
{
    [InvariantReturn]
    public partial class ProductCategoryTableAdapter : ISqlServerConnectionProvider
    {
        public SqlConnection SqlConnection
        {
            get { return this.Connection; }
        }
    }
}