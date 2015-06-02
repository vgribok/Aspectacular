#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Transactions;

namespace Aspectacular
{
    /// <summary>
    ///     Aspect implementing DTC transaction.
    ///     It's slow. Use only when updating data,
    ///     i.e. apply to Create/Update/Delete multi-database methods,
    ///     by using [RequiredAspectAttribute] on the method-by-method basis.
    /// </summary>
    public class DtcTransactionAspect : Aspect, IDisposable
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
            this.options = new TransactionOptions {IsolationLevel = isolationLevel, Timeout = waitTimeout};
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationWithKey("Starting DTC transaction", "Scope = {0}, Isolation Level = {1}, Timeout = {2}", this.transationScope, this.options.IsolationLevel, this.options.Timeout);
            this.transaction = new TransactionScope(this.transationScope, this.options);
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if(!interceptedCallSucceeded)
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

            this.Dispose(); // Rolls transaction back if not committed.
        }

        public void Dispose()
        {
            if(this.transaction != null)
            {
                this.transaction.Dispose(); // Rolls transaction back if not committed.
                this.transaction = null;
            }
        }
    }

    /// <summary>
    ///     Shortcut attribute applied to methods to mark them as requiring DTC transaction aspect.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequiredDtcTransactionAspectAttribute : RequiredAspectAttribute
    {
        public RequiredDtcTransactionAspectAttribute(
            TransactionScopeOption transationScope = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadUncommitted, int timeoutMillisecond = -1
            )
            : base(typeof(DtcTransactionAspect), WhenRequiredAspectIsMissing.InstantiateAndAppend, transationScope, isolationLevel,
                timeoutMillisecond < 1 ? new TimeSpan() : new TimeSpan(0, 0, 0, 0, timeoutMillisecond))
        {
        }
    }
}