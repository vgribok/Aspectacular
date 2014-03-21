using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Aspectacular
{
    public static class SqlUtils
    {
        /// <summary>
        /// SQL commands injected before queries to improve query performance.
        /// To avoid using them, just set the value of SqlConnectionAttributes = null;
        /// Feel free to specify any commands you wish - they will be executed
        /// every time SQL Server connection is opened by EF AOP Proxies.
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
        /// Adds SQL Server command attributes which improves query performance.
        /// </summary>
        /// <param name="sqlConn"></param>
        public static void AttachSqlConnectionAttribs(this SqlConnection sqlConn)
        {
            if (sqlConn != null)
                sqlConn.StateChange += new StateChangeEventHandler(OnSqlConnectionOpen);
        }

        public static void OnSqlConnectionOpen(object sender, StateChangeEventArgs e)
        {
            if (SqlConnectionAttributes == null)
                return;

            if (e.CurrentState != ConnectionState.Open)
                return;

            SqlConnection sqlConn = (SqlConnection)sender;

            using (SqlCommand cmd = sqlConn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = SqlConnectionAttributes;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
