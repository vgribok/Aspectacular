using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.DbCtx
{
    [InvariantReturn]
    public partial class AdventureWorksLT2008R2Entities : IEfCallInterceptor, ICallLogger, ISqlServerConnectionProvider
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
