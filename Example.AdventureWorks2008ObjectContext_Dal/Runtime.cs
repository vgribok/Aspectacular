using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Aspectacular;
using Value.Framework.Aspectacular.Data;

namespace Example.AdventureWorks2008ObjectContext_Dal
{
    [InvariantReturn]
    public partial class AdventureWorksLT2008R2Entities : IEfCallInterceptor
    {
        public int SaveChangeReturnValue { get; set; }
    }
}
