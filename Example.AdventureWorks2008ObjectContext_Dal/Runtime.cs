using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Aspectacular;

namespace Example.AdventureWorks2008ObjectContext_Dal.DbCtx
{
    [InvariantReturn]
    public partial class AdventureWorksLT2008R2Entities : IEfCallInterceptor, ICallLogger
    {
        public int SaveChangeReturnValue { get; set; }

        public IMethodLogProvider AopLogger { get; set; }
    }
}

namespace Example.AdventureWorks2008ObjectContext_Dal.ObjCtx
{
    //[InvariantReturn]
    //public partial class AdventureWorksLT2008R2EntitiesObjCtx : IEfCallInterceptor, ICallLogger
    //{
    //    public int SaveChangeReturnValue { get; set; }

    //    public IMethodLogProvider AopLogger { get; set; }
    //}
}
