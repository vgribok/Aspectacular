#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Data;
using System.Data.SqlClient;

namespace Aspectacular
{
    public static class SqlUtils
    {
        /// <summary>
        ///     SQL commands injected before queries to improve query performance.
        ///     To avoid using them, just set the value of SqlConnectionAttributes = null;
        ///     Feel free to specify any commands you wish - they will be executed
        ///     every time SQL Server connection is opened by EF AOP Proxies.
        /// </summary>
        public static NonEmptyString SqlConnectionAttributes =
            "SET ANSI_NULLS ON;\r\n" +
            "SET ANSI_PADDING ON;\r\n" +
            "SET ANSI_WARNINGS ON;\r\n" +
            "SET CONCAT_NULL_YIELDS_NULL ON;\r\n" +
            "SET QUOTED_IDENTIFIER ON;\r\n" +
            "SET NUMERIC_ROUNDABORT OFF;\r\n" +
            "SET ARITHABORT ON;\r\n"; // Improves performance a lot (matches SQL Studio), produces much execution plans. More at http://technet.microsoft.com/en-us/library/ms190306.aspx

        /// <summary>
        ///     Adds SQL Server command attributes which improves query performance.
        /// </summary>
        /// <param name="sqlConn"></param>
        /// <param name="optionalPostProcessingFunc"></param>
        public static void AttachSqlConnectionAttribs(this SqlConnection sqlConn, Action<SqlConnection> optionalPostProcessingFunc = null)
        {
            if(sqlConn == null)
                return;

            sqlConn.StateChange += (sender, e) =>
            {
                if(SqlConnectionAttributes == null)
                    return;

                if(e.CurrentState != ConnectionState.Open)
                    return;

                SqlConnection sqlConnection = (SqlConnection)sender;

                using(SqlCommand cmd = sqlConnection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = SqlConnectionAttributes;
                    cmd.ExecuteNonQuery();
                }

                if(optionalPostProcessingFunc != null)
                    optionalPostProcessingFunc(sqlConnection);
            };
        }
    }
}