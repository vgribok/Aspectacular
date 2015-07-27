#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.SqlClient;

namespace Aspectacular
{
    /// <summary>
    ///     Alloc/invoke/dispose convenience class for EF DbContext subclasses.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    public class DbContextSingleCallProxy<TDbContext> : DbEngineProxy<TDbContext>, ISqlServerConnectionProvider
        where TDbContext : DbContext, new()
    {
        private readonly bool? lazyLoading;

        public DbContextSingleCallProxy(IEnumerable<Aspect> aspects, bool lazyLoadingEnabled = true)
            : base(aspects)
        {
            this.lazyLoading = lazyLoadingEnabled;
        }

        /// <summary>
        ///     A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="aspects"></param>
        /// <param name="autoDispose">If true, Dispose() will be called after the end of the intercepted call.</param>
        public DbContextSingleCallProxy(TDbContext dbContext, IEnumerable<Aspect> aspects, bool autoDispose = false)
            : base(dbContext, aspects, autoDispose)
        {
        }

        protected override void Step_2_BeforeTryingMethodExec()
        {
            if(this.lazyLoading != null)
                this.AugmentedClassInstance.Configuration.LazyLoadingEnabled = this.lazyLoading.Value;

            base.Step_2_BeforeTryingMethodExec();
        }

        public override int CommitChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        /// <summary>
        ///     Implements ISqlServerConnectionProvider.
        ///     Returns SqlConnection, if Context is for SQL Server.
        /// </summary>
        public SqlConnection SqlConnection
        {
            get { return this.AugmentedClassInstance.GetSqlConnection(); }
        }
    }

    /// <summary>
    ///     Alloc/invoke/dispose convenience class for EF ObjectContext subclasses.
    /// </summary>
    /// <typeparam name="TObjectContext"></typeparam>
    public class ObjectContextSingleCallProxy<TObjectContext> : DbEngineProxy<TObjectContext>, ISqlServerConnectionProvider
        where TObjectContext : ObjectContext, new()
    {
        private readonly bool? lazyLoading;

        public ObjectContextSingleCallProxy(IEnumerable<Aspect> aspects, bool lazyLoadingEnabled = true)
            : base(aspects)
        {
            this.lazyLoading = lazyLoadingEnabled;
        }

        /// <summary>
        ///     A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="ocContext"></param>
        /// <param name="aspects"></param>
        public ObjectContextSingleCallProxy(TObjectContext ocContext, IEnumerable<Aspect> aspects)
            : base(ocContext, aspects)
        {
        }

        protected override void Step_2_BeforeTryingMethodExec()
        {
            if(this.lazyLoading != null)
                this.AugmentedClassInstance.ContextOptions.LazyLoadingEnabled = this.lazyLoading.Value;

            base.Step_2_BeforeTryingMethodExec();
        }

        public override int CommitChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        /// <summary>
        ///     Implements ISqlServerConnectionProvider.
        /// </summary>
        public SqlConnection SqlConnection
        {
            get { return this.AugmentedClassInstance.GetSqlConnection(); }
        }
    }

    /// <summary>
    ///     Factory class for supplying Entity Framework AOP proxies.
    /// </summary>
// ReSharper disable once InconsistentNaming
    public static class EfAOP
    {
        /// <summary>
        ///     Returns AOP proxy for EF DbContext class that will instantiate DbContext and after usage calls DbContext.Dispose()
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="lazyLoadingEnabled"></param>
        /// <returns></returns>
        public static DbContextSingleCallProxy<TDbContext> GetDbProxy<TDbContext>(IEnumerable<Aspect> aspects = null, bool lazyLoadingEnabled = true)
            where TDbContext : DbContext, new()
        {
            var proxy = new DbContextSingleCallProxy<TDbContext>(aspects, lazyLoadingEnabled);
            return proxy;
        }

        /// <summary>
        ///     Returns AOP proxy for EF ObjectContext class
        /// </summary>
        /// <typeparam name="TObjectContext"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="lazyLoadingEnabled"></param>
        /// <returns></returns>
        public static ObjectContextSingleCallProxy<TObjectContext> GetOcProxy<TObjectContext>(IEnumerable<Aspect> aspects = null, bool lazyLoadingEnabled = true)
            where TObjectContext : ObjectContext, new()
        {
            var proxy = new ObjectContextSingleCallProxy<TObjectContext>(aspects, lazyLoadingEnabled);
            return proxy;
        }
    }

    public static class EfAopExts
    {
        /// <summary>
        ///     Returns InstanceProxy[TDbContext] for DbContext instance that already exist.
        ///     Returned proxy won't call DbContext.Dispose() after method invocation.
        ///     Supports ExecuteCommand() method.
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="dbExistingContext"></param>
        /// <param name="aspects"></param>
        /// <param name="autoDispose">If true, Dispose() will be called after the end of the intercepted call.</param>
        /// <returns></returns>
        public static DbContextSingleCallProxy<TDbContext> GetDbProxy<TDbContext>(this TDbContext dbExistingContext, IEnumerable<Aspect> aspects = null, bool autoDispose = false)
            where TDbContext : DbContext, new()
        {
            var proxy = new DbContextSingleCallProxy<TDbContext>(dbExistingContext, aspects, autoDispose);
            return proxy;
        }

        /// <summary>
        ///     Returns InstanceProxy[TObjectContext] for ObjectContext instance that already exist.
        ///     Returned proxy won't call ObjectContext.Dispose() after method invocation.
        ///     Supports ExecuteCommand() method.
        /// </summary>
        /// <typeparam name="TObjectContext"></typeparam>
        /// <param name="existingOcContext"></param>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static ObjectContextSingleCallProxy<TObjectContext> GetOcProxy<TObjectContext>(this TObjectContext existingOcContext, IEnumerable<Aspect> aspects = null)
            where TObjectContext : ObjectContext, new()
        {
            var proxy = new ObjectContextSingleCallProxy<TObjectContext>(existingOcContext, aspects);
            return proxy;
        }
    }
}