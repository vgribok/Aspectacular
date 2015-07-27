#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Aspectacular
{
    /// <summary>
    ///     Base class for proxies dealing with EF, ADO.NET and other connection-based data access engines.
    /// </summary>
    /// <typeparam name="TDbEngine"></typeparam>
    public abstract class DbEngineProxy<TDbEngine> : AllocateRunDisposeProxy<TDbEngine>, IEfCallInterceptor, IStorageCommandRunner<TDbEngine>
        where TDbEngine : class, IDisposable, new()
    {
        protected DbEngineProxy(IEnumerable<Aspect> aspects)
            : base(aspects)
        {
        }

        /// <summary>
        ///     A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="aspects"></param>
        /// <param name="autoDispose">If true, Dispose() will be called after the end of the intercepted call.</param>
        protected DbEngineProxy(TDbEngine dbContext, IEnumerable<Aspect> aspects, bool autoDispose = false)
            : base(dbContext, aspects, autoDispose)
        {
        }

        #region Base class overrides

        protected override void InvokeActualInterceptedMethod(Action interceptedMethodClosure)
        {
            base.InvokeActualInterceptedMethod(interceptedMethodClosure);
            this.SaveChanges();
        }

        #endregion Base class overrides

        #region Implementation of IEfCallInterceptor

        public int SaveChangeReturnValue { get; set; }

        public int SaveChanges()
        {
            if(this.AugmentedClassInstance == null) // SaveChanges() called directly, like db.GetDbProxy().SaveChanges();
                return this.ExecuteCommand(db => SaveChangesDirect());

            this.SaveChangeReturnValue = this.CommitChanges();

            this.LogInformationData("SaveChanges() result", this.SaveChangeReturnValue);

            //if (this.InterceptedCallMetaData.MethodReturnType.Equals(typeof(void)))
            //    this.ReturnedValue = this.SaveChangeReturnValue;

            return this.SaveChangeReturnValue;
        }

        #endregion Implementation of IEfCallInterceptor

        #region Implementation of IStorageCommandRunner

        /// <summary>
        ///     Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public virtual int ExecuteCommand(Expression<Action<TDbEngine>> callExpression)
        {
            this.Invoke(callExpression);
            return this.SaveChangeReturnValue;
        }

        #endregion IStorageCommandRunner

        /// <summary>
        ///     Subclasses' implementation of CommitChanges() should call DbContext.SaveChanges().
        /// </summary>
        /// <returns></returns>
        public abstract int CommitChanges();

        #region Utility Methods

        /// <summary>
        ///     Do-nothing method that facilitates calling db.GetDbProxy().SaveChanges();
        /// </summary>
        private static void SaveChangesDirect()
        {
        }

        #endregion Utility Methods
    }

    //public class AdoNetProxy<TDbConnection> : DbEngineProxy<TDbConnection>
    //    where TDbConnection : class, IDbConnection, new()
    //{
    //    public IDbCommand DbCommand { get; protected set; }

    //    public AdoNetProxy(IEnumerable<Aspect> aspects)
    //        : base(aspects)
    //    {
    //    }

    //    public AdoNetProxy(TDbConnection dbConnection, IEnumerable<Aspect> aspects)
    //        : base(dbConnection, aspects)
    //    {
    //    }

    //    public int ExecuteCommand(CommandType cmdType, string cmdText, params IDbDataParameter[] args)
    //    {
    //        this.CreateDbCommand(cmdType, cmdText, args);

    //        int retVal = this.DbCommand.ExecuteNonQuery();
    //        return retVal;
    //    }

    //    private void CreateDbCommand(CommandType cmdType, string cmdText, IDbDataParameter[] args)
    //    {
    //        if (cmdText.IsBlank())
    //            throw new ArgumentNullException("cmdText");

    //        this.DbCommand = this.AugmentedClassInstance.CreateCommand();
    //        this.DbCommand.CommandType = cmdType;
    //        this.DbCommand.CommandText = cmdText;

    //        args.ForEach(parm => this.DbCommand.Parameters.Add(parm));
    //    }
    //}
}