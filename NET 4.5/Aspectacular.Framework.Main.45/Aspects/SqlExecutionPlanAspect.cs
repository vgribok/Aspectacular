#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Data;
using System.Data.SqlClient;

namespace Aspectacular
{
    /// <summary>
    ///     Logs T-SQL execution plan of a SqlCommand.
    /// </summary>
    public class SqlCmdExecutionPlanAspect : Aspect
    {
        protected SqlCommand command = null;
        protected readonly Func<object, SqlCommand> cmdRetriever;

        /// <summary>
        ///     Aspect that log SQL Execution plan of an intercepted SqlCommand.
        /// </summary>
        /// <param name="formatXmlOrText">
        ///     string in the format of "format=text;" or "format=xml;".
        ///     If not specified, XML format is used.
        /// </param>
        public SqlCmdExecutionPlanAspect(string formatXmlOrText)
            : this(cmdFetcher: null)
        {
            if(!formatXmlOrText.IsBlank())
                this.TrueText_FalseXml = bool.Parse(DefaultAspect.GetParameterValue(formatXmlOrText, "format", "false"));
        }

        /// <summary>
        ///     Aspect that log SQL Execution plan of a SqlCommand.
        /// </summary>
        /// <param name="cmdFetcher">Optional SqlCommand factory. If not specified, intercepted object should be SqlCommand.</param>
        /// <param name="trueText_falseXml">Specifies output format of the execution plan text: plain text or XML.</param>
        public SqlCmdExecutionPlanAspect(Func<object, SqlCommand> cmdFetcher = null, bool trueText_falseXml = false)
        {
            this.cmdRetriever = cmdFetcher ?? (obj => obj as SqlCommand);
            this.TrueText_FalseXml = trueText_falseXml;
        }

// ReSharper disable once InconsistentNaming
        public bool TrueText_FalseXml { get; set; }

        public override void Step_2_BeforeTryingMethodExec()
        {
            if(this.Proxy.AugmentedClassInstance == null)
                return;

            this.command = this.cmdRetriever(this.Proxy.AugmentedClassInstance);
            if(this.command == null)
                return;

            if(this.command.Connection == null)
                throw new Exception("SQL command should have its Connection specified in order for Execution Plan aspect to work.");

            if(this.command.Connection.State == ConnectionState.Open)
                this.OnConnectionOpened();
            else
                this.command.Connection.StateChange += this.OnConnectionStateChange;
        }

        private void OnConnectionStateChange(object sender, StateChangeEventArgs e)
        {
            if(e.CurrentState == ConnectionState.Open)
                this.OnConnectionOpened();
        }

        private void OnConnectionOpened()
        {
            string outputFormat = this.TrueText_FalseXml ? "text" : "xml";

            SqlCommand showplanCmd = new SqlCommand("set showplan_{0} on".SmartFormat(outputFormat), this.command.Connection);
            showplanCmd.ExecuteNonQuery();

            string executionPlan = null;
            using(SqlDataReader showplan_results = this.command.ExecuteReader())
            {
                if(showplan_results.Read())
                    executionPlan = showplan_results[0].ToString();
            }

            if(executionPlan != null)
                this.ProcessExecutionPlan(executionPlan);

            SqlCommand showplan_off_cmd = new SqlCommand("set showplan_{0} off".SmartFormat(outputFormat), this.command.Connection);
            showplan_off_cmd.ExecuteNonQuery();
        }

        /// <summary>
        ///     Do something with the execution plan.
        ///     Default action is to log it.
        /// </summary>
        /// <param name="executionPlan"></param>
        protected virtual void ProcessExecutionPlan(string executionPlan)
        {
            this.LogInformationData("T-SQL Execution Plan", executionPlan);
        }
    }
}