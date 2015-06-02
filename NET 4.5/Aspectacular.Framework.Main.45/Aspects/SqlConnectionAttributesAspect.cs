#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Data.SqlClient;

namespace Aspectacular
{
    /// <summary>
    ///     If your context class (TableAdapter, DbContext or ObjectContext) is interfacing with SQL Server,
    ///     then you may improve performance of the queries by implementing this interface.
    /// </summary>
    public interface ISqlServerConnectionProvider
    {
        SqlConnection SqlConnection { get; }
    }

    /// <summary>
    ///     Can be implemented by multi-database lazily-initialized connection managers.
    /// </summary>
    public interface ISqlServerMultiConnectionProvider
    {
        Action<ISqlServerConnectionProvider> SqlConnectionAttributeApplicator { get; set; }
    }

    /// <summary>
    ///     Aspect improving SQL server query performance by adding
    ///     connection attributes when SQL connection is initialized.
    ///     Make intercepted object implement ISqlServerConnectionProvider
    ///     interface to take advantage of this aspect.
    /// </summary>
    public class SqlConnectionAttributesAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            base.Step_2_BeforeTryingMethodExec();

            if(SqlUtils.SqlConnectionAttributes == null)
                return;

            ISqlServerConnectionProvider sqlConnProvider = (this.Proxy.AugmentedClassInstance as ISqlServerConnectionProvider) ?? (this.Proxy as ISqlServerConnectionProvider);
            this.AttachSqlAttributes(sqlConnProvider);

            ISqlServerMultiConnectionProvider sqlConnMultiConProvider = (this.Proxy.AugmentedClassInstance as ISqlServerMultiConnectionProvider) ?? (this.Proxy as ISqlServerMultiConnectionProvider);
            if(sqlConnMultiConProvider != null)
            {
                sqlConnMultiConProvider.SqlConnectionAttributeApplicator = this.AttachSqlAttributes;
            }
        }

        private void AttachSqlAttributes(ISqlServerConnectionProvider sqlConnProvider)
        {
            if(sqlConnProvider != null)
                sqlConnProvider.SqlConnection.AttachSqlConnectionAttribs(sqlConn => this.LogAttributeApplication(sqlConnProvider));
        }

        /// <summary>
        ///     Attention: this is a callback method that could be called after aspect is used & dumped.
        /// </summary>
        /// <param name="sqlConnProvider"></param>
        private void LogAttributeApplication(ISqlServerConnectionProvider sqlConnProvider)
        {
            if(this.Proxy == null)
                return;

            this.LogInformationWithKey("Injected SQL Connection Attributes",
                "Added following SQL connection attributes to \"{0}\":\r\n{1}", sqlConnProvider.GetType().FormatCSharp(), SqlUtils.SqlConnectionAttributes);
        }
    }
}