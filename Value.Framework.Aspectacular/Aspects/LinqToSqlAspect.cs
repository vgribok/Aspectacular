using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Aspect allowing capturing SQL from Entity Framework queries
    /// when intercepted method returns IQueryable/ObjectQuery result.
    /// </summary>
    public class LinqToSqlAspect : Aspect
    {
        /// <summary>
        /// IQueryable converted to T-SQL, when used with EntityFramework,
        /// or expression trace when used not with EntityFramework.
        /// </summary>
        public string SQLorTrace { get; protected set; }

        public override void Step_3_BeforeMassagingReturnedResult()
        {
            ObjectQuery query = this.Proxy.ReturnedValue as ObjectQuery;

            if (query != null)
                this.SetSql(query.ToTraceString());
            else
            {
                IQueryable q = this.Proxy.ReturnedValue as IQueryable;
                if (q != null)
                    this.SetSql(q.ToString());
            }
        }

        private void SetSql(string sql)
        {
            this.SQLorTrace = ("\r\n" + sql).Replace("\r\n", "\r\n\t");
            this.LogInformationData("SQL", this.SQLorTrace);
        }
    }
}
