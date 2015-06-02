#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Data.Objects;
using System.Linq;

namespace Aspectacular
{
    /// <summary>
    ///     Aspect allowing capturing SQL from Entity Framework queries
    ///     when intercepted method returns IQueryable/ObjectQuery result.
    /// </summary>
    public class LinqToSqlAspect : Aspect
    {
        /// <summary>
        ///     IQueryable converted to T-SQL, when used with EntityFramework,
        ///     or expression trace when used not with EntityFramework.
        /// </summary>
// ReSharper disable once InconsistentNaming
        public string SQLorTrace { get; protected set; }

        public override void Step_3_BeforeMassagingReturnedResult()
        {
            ObjectQuery query = this.Proxy.ReturnedValue as ObjectQuery;

            if(query != null)
                this.SetSql(query.ToTraceString());
            else
            {
                IQueryable q = this.Proxy.ReturnedValue as IQueryable;
                if(q != null)
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