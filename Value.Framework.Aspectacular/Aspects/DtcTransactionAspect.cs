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
            this.transaction = new TransactionScope(this.transationScope, this.options);
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if(interceptedCallSucceeded)
                this.transaction.Complete();

            this.transaction.Dispose(); // Rolls transaction back if not committed.
            this.transaction = null;
        }
    }
}
