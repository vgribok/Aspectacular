#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Data.SqlClient;
using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.DbCtx
{
    [InvariantReturn]
    partial class AdventureWorksLT2008R2Entities : IEfCallInterceptor, ICallLogger, ISqlServerConnectionProvider
    {
        public int SaveChangeReturnValue { get; set; }

        public IMethodLogProvider AopLogger { get; set; }

        public SqlConnection SqlConnection
        {
            get { return this.GetSqlConnection(); }
        }
    }
}

namespace Example.AdventureWorks2008ObjectContext_Dal.ObjCtx
{
    //[InvariantReturn]
    //public partial class AdventureWorksLT2008R2EntitiesObjCtx : IEfCallInterceptor, ICallLogger, ISqlServerConnectionProvider
    //{
    //    public int SaveChangeReturnValue { get; set; }

    //    public IMethodLogProvider AopLogger { get; set; }

    //    public System.Data.SqlClient.SqlConnection SqlConnection
    //    {
    //        get { return this.GetSqlConnection(); }
    //    }
    //}
}