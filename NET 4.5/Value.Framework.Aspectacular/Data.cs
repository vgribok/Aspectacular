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
    ///     Interface of the non-select database storage command execution.
    /// </summary>
    /// <typeparam name="TCmd"></typeparam>
    public interface IStorageCommandRunner<TCmd>
        where TCmd : class, IDisposable, new()
    {
        /// <summary>
        ///     Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns>Value returned by DB engines like SQL server when insert and update statements are run that return no value</returns>
        int ExecuteCommand(Expression<Action<TCmd>> callExpression);
    }

    /// <summary>
    ///     Entity Framework context marker interface.
    ///     (Can be implemented by both EF proxies and EF DbContext & ObjectContext classes.
    /// </summary>
    public interface IEfCallInterceptor
    {
        /// <summary>
        ///     Result of ObjectContex or DbContext .SaveChanges() call.
        /// </summary>
        int SaveChangeReturnValue { get; set; }

        /// <summary>
        ///     Proxy for calling DbContext and ObjectContext SaveChanges() method.
        /// </summary>
        /// <returns></returns>
        int SaveChanges();
    }

    /// <summary>
    ///     Base class for BL API- and AOP-friendly data store/connection managers,
    ///     supporting multiple heterogeneous data stores, like ADO.NET, EF (DbContext and ObjectContext), NoSQL,
    ///     and basically any other type.
    /// </summary>
    /// <remarks>
    ///     Subclass this class or its subclasses (TwoDataStoreManager, TthreeDataStoreManager, etc.)
    ///     and write DAL/BL methods spanning multiple data stores as instance members of the class.
    ///     Since data context/connection classes are often expensive to instantiate, this class
    ///     employs lazy connection/context creation, meaning that even when any of your DAL/BL
    ///     methods uses, say, two out three DAL context classes, those two will be instantiated, while third one will not.
    ///     When using EF DbContext or ObjectContext as TDataStore, please make those classes
    ///     implement IEfCallInterceptor interface by adding "public int SaveChangeReturnValue { get; set; }"
    ///     to its definition, so that SaveChanges() would be called on all contexts.
    /// </remarks>
    public abstract class DalManager : IEfCallInterceptor, IDisposable, ICallLogger, ISqlServerMultiConnectionProvider
    {
        private readonly Dictionary<Type, Lazy<IDisposable>> dataStores = new Dictionary<Type, Lazy<IDisposable>>();

        protected void AddDataStoreInitializer<TDataStore>()
            where TDataStore : class, IDisposable, new()
        {
            this.dataStores[typeof(TDataStore)] = new Lazy<IDisposable>(isThreadSafe: true, valueFactory: this.CreateDataStoreInstance<TDataStore>);
        }

        private TDataStore CreateDataStoreInstance<TDataStore>()
            where TDataStore : class, IDisposable, new()
        {
            var db = this.Instantiate<TDataStore>();

            if(this.SqlConnectionAttributeApplicator != null)
                this.SqlConnectionAttributeApplicator(db as ISqlServerConnectionProvider);

            return db;
        }

        public virtual TDataStore Instantiate<TDataStore>()
            where TDataStore : class, IDisposable, new()
        {
            return new TDataStore();
        }

        protected bool IsStoreInitialized<TDataStore>()
            where TDataStore : class, IDisposable, new()
        {
            Lazy<IDisposable> lazyProxy;
            if(!this.dataStores.TryGetValue(typeof(TDataStore), out lazyProxy))
            {
                this.AddDataStoreInitializer<TDataStore>();
                return false;
            }

            if(!lazyProxy.IsValueCreated)
                return false;

            return true;
        }

        /// <summary>
        ///     Ensures that expensive data context/connection classes are not instantiated unnecessarily.
        /// </summary>
        /// <typeparam name="TDataStore"></typeparam>
        /// <returns></returns>
        protected TDataStore LazyGetDataStoreReference<TDataStore>()
            where TDataStore : class, IDisposable, new()
        {
            Lazy<IDisposable> lazyProxy;
            if(!this.dataStores.TryGetValue(typeof(TDataStore), out lazyProxy))
                return null;

            return (TDataStore)lazyProxy.Value;
        }

        #region Implementation of IEfCallInterceptor

        public int SaveChangeReturnValue { get; set; }

        public int SaveChanges()
        {
            int total = 0;

            this.dataStores.ForEach(pair =>
            {
                Lazy<IDisposable> dataStoreProxy = pair.Value;

                if(dataStoreProxy.IsValueCreated)
                {
                    IEfCallInterceptor efContext = dataStoreProxy.Value as IEfCallInterceptor;
                    if(efContext != null)
                    {
                        efContext.SaveChangeReturnValue = efContext.SaveChanges();
                        total += efContext.SaveChangeReturnValue;
                    }
                }
            });

            return total;
        }

        #endregion Implementation of IEfCallInterceptor

        public void Dispose()
        {
            this.dataStores.ForEach(pair =>
            {
                Lazy<IDisposable> dataStoreProxy = pair.Value;

                if(dataStoreProxy.IsValueCreated)
                    dataStoreProxy.Value.Dispose();
            });

            this.dataStores.Clear();
        }

        public IMethodLogProvider AopLogger { get; set; }

        /// <summary>
        ///     This value may be set by a SqlConnectionAttributesAspect to improve SQL Server query performance.
        /// </summary>
        public Action<ISqlServerConnectionProvider> SqlConnectionAttributeApplicator { get; set; }
    }

    #region Convenience intermediate base classes derived from DataStoreManager

    public abstract class TwoDataStoreManager<TStore1, TStore2> : DalManager
        where TStore1 : class, IDisposable, new()
        where TStore2 : class, IDisposable, new()
    {
        protected TwoDataStoreManager()
        {
            this.AddDataStoreInitializer<TStore1>();
            this.AddDataStoreInitializer<TStore2>();
        }
    }

    public abstract class ThreeDataStoreManager<TStore1, TStore2, TStore3> : DalManager
        where TStore1 : class, IDisposable, new()
        where TStore2 : class, IDisposable, new()
        where TStore3 : class, IDisposable, new()
    {
        protected ThreeDataStoreManager()
        {
            this.AddDataStoreInitializer<TStore1>();
            this.AddDataStoreInitializer<TStore2>();
            this.AddDataStoreInitializer<TStore3>();
        }
    }

    public abstract class FourDataStoreManager<TStore1, TStore2, TStore3, TStore4> : DalManager
        where TStore1 : class, IDisposable, new()
        where TStore2 : class, IDisposable, new()
        where TStore3 : class, IDisposable, new()
        where TStore4 : class, IDisposable, new()
    {
        protected FourDataStoreManager()
        {
            this.AddDataStoreInitializer<TStore1>();
            this.AddDataStoreInitializer<TStore2>();
            this.AddDataStoreInitializer<TStore3>();
            this.AddDataStoreInitializer<TStore4>();
        }
    }

    #endregion Convenience intermediate base classes derived from DataStoreManager

    #region Examples and common uses of DataStoreManager base class

    public class OltpAndWarehouse<TOltpStore, TWarehouseStore> : TwoDataStoreManager<TOltpStore, TWarehouseStore>
        where TOltpStore : class, IDisposable, new()
        where TWarehouseStore : class, IDisposable, new()
    {
        protected TOltpStore Oltp
        {
            get { return this.LazyGetDataStoreReference<TOltpStore>(); }
        }

        protected TWarehouseStore Whouse
        {
            get { return this.LazyGetDataStoreReference<TWarehouseStore>(); }
        }
    }

    public class OltpWarehouseAndOlap<TOltpStore, TWarehouseStore, TOlapStore> : ThreeDataStoreManager<TOltpStore, TWarehouseStore, TOlapStore>
        where TOltpStore : class, IDisposable, new()
        where TWarehouseStore : class, IDisposable, new()
        where TOlapStore : class, IDisposable, new()
    {
        protected TOltpStore Oltp
        {
            get { return this.LazyGetDataStoreReference<TOltpStore>(); }
        }

        protected TWarehouseStore Whouse
        {
            get { return this.LazyGetDataStoreReference<TWarehouseStore>(); }
        }

        protected TOlapStore Olap
        {
            get { return this.LazyGetDataStoreReference<TOlapStore>(); }
        }
    }

    #endregion Examples and common uses of DataStoreManager base class
}