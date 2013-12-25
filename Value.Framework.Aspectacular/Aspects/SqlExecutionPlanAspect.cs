using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular.Aspects
{
    public interface ISqlCmdRetriever
    {
        SqlCommand GetCommand(object interceptedObject);
    }

    public abstract class SqlExecutionPlanAspect : Aspect
    {
        protected SqlCommand command = null;
        protected readonly ISqlCmdRetriever cmdRetriever;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmdFetcher"></param>
        /// <param name="formatXmlOrText">string in the format of "format=xml;"</param>
        public SqlExecutionPlanAspect(ISqlCmdRetriever cmdFetcher, string formatXmlOrText)
        {
            this.cmdRetriever = cmdFetcher;

            if (!formatXmlOrText.IsBlank())
                this.TrueText_FalseXml = bool.Parse(DefaultAspect.GetParameterValue(formatXmlOrText, "format", "false"));
        }

        public bool TrueText_FalseXml { get; set; }

        public override void Step_2_BeforeTryingMethodExec()
        {
            if (this.Proxy.AugmentedClassInstance == null)
                return;

            this.command = this.cmdRetriever.GetCommand(this.Proxy.AugmentedClassInstance);
            if (this.command == null)
                return;

            if (this.command.Connection == null)
                throw new Exception("SQL command should have its Connection specified in order for Execution Plan aspect to work.");

            if (this.command.Connection.State == ConnectionState.Open)
                this.OnConnectionOpened();
            else
                this.command.Connection.StateChange += this.OnConnectionStateChange;
        }

        void OnConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            if (e.CurrentState == ConnectionState.Open)
                this.OnConnectionOpened();
        }

        private void OnConnectionOpened()
        {
            string outputFormat = this.TrueText_FalseXml ? "text" : "xml";

            SqlCommand showplanCmd = new SqlCommand("set showplan_{0} on".SmartFormat(outputFormat), this.command.Connection);
            showplanCmd.ExecuteNonQuery();

            using (SqlDataReader showplan_results = this.command.ExecuteReader())
            {
                if (showplan_results.Read())
                {
                    string executionPlan = showplan_results[0].ToString();
                    this.LogInformationData("T-SQL Execution Plan", executionPlan);
                }
            }

            SqlCommand showplan_off_cmd = new SqlCommand("set showplan_{0} off".SmartFormat(outputFormat), this.command.Connection);
            showplanCmd.ExecuteNonQuery();
        }
    }
}
