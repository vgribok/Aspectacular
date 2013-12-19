using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Value.Framework.Aspectacular.Aspects
{
    /// <summary>
    /// Aspect implementing DTC transaction.
    /// It's slow. Use only when updating data, 
    /// i.e. apply to Create/Update/Delete multi-database methods,
    /// by using [RequiredAspectAttribute] on the method-by-method basis.
    /// </summary>
    public class DtcTransactionAspect : Aspect
    {
        protected TransactionScope transaction = null;

        protected readonly TransactionScopeOption transationScope;
        protected readonly TransactionOptions options;

        public DtcTransactionAspect()
            : this(TransactionScopeOption.Required)
        {
        }

        public DtcTransactionAspect(TransactionScopeOption transationScope, IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted, TimeSpan waitTimeout = new TimeSpan())
        {
            this.transationScope = transationScope;
            this.options = new TransactionOptions() { IsolationLevel = isolationLevel, Timeout = waitTimeout };
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationWithKey("Starting DTC transaction", "Scope = {0}, Isolation Level = {1}, Timeout = {2}", this.transationScope, this.options.IsolationLevel, this.options.Timeout);
            this.transaction = new TransactionScope(this.transationScope, this.options);
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if (!interceptedCallSucceeded)
                this.LogInformation("Rolling back DTC transaction.");
            else
            {
                bool success = false;
                try
                {
                    this.transaction.Complete();
                    success = true;
                }
                finally
                {
                    this.LogInformationData("Transaction committed", success);
                }
            }

            this.transaction.Dispose(); // Rolls transaction back if not committed.
            this.transaction = null;
        }
    }
}
