using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Alloc/invoke/dispose convenience class for EF DbContext subclasses.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    public class DbContextSingleCallProxy<TDbContext> : DbEngineProxy<TDbContext>
            where TDbContext : DbContext, new()
    {
        private readonly bool? lazyLoading = null;

        public DbContextSingleCallProxy(IEnumerable<Aspect> aspects, bool lazyLoadingEnabled = true)
            : base(aspects)
        {
            this.lazyLoading = lazyLoadingEnabled;
        }

        /// <summary>
        /// A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="aspects"></param>
        public DbContextSingleCallProxy(TDbContext dbContext, IEnumerable<Aspect> aspects)
            : base(dbContext, aspects)
        {
        }

        protected override SqlConnection GetSqlConnection()
        {
            SqlConnection sqlConnection = this.AugmentedClassInstance.Database.Connection as SqlConnection;
            return sqlConnection;
        }

        protected override void Step_2_BeforeTryingMethodExec()
        {
            if (this.lazyLoading != null)
                this.AugmentedClassInstance.Configuration.LazyLoadingEnabled = this.lazyLoading.Value;

            base.Step_2_BeforeTryingMethodExec();
        }

        public override int CommitChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }
    }

    /// <summary>
    /// Alloc/invoke/dispose convenience class for EF ObjectContext subclasses.
    /// </summary>
    /// <typeparam name="TObjectContext"></typeparam>
    public class ObjectContextSingleCallProxy<TObjectContext> : DbEngineProxy<TObjectContext>
            where TObjectContext : ObjectContext, new()
    {
        private readonly bool? lazyLoading = null;

        public ObjectContextSingleCallProxy(IEnumerable<Aspect> aspects, bool lazyLoadingEnabled = true)
            : base(aspects)
        {
            this.lazyLoading = lazyLoadingEnabled;
        }

        /// <summary>
        /// A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="ocContext"></param>
        /// <param name="aspects"></param>
        public ObjectContextSingleCallProxy(TObjectContext ocContext, IEnumerable<Aspect> aspects)
            : base(ocContext, aspects)
        {
        }

        protected override SqlConnection GetSqlConnection()
        {
            var entityConnection = this.AugmentedClassInstance.Connection as System.Data.EntityClient.EntityConnection;
            if (entityConnection == null)
                return null;

            var sqlConnection = entityConnection.StoreConnection as SqlConnection;
            return sqlConnection;
        }

        protected override void Step_2_BeforeTryingMethodExec()
        {
            if (this.lazyLoading != null)
                this.AugmentedClassInstance.ContextOptions.LazyLoadingEnabled = this.lazyLoading.Value;

            base.Step_2_BeforeTryingMethodExec();
        }

        public override int CommitChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }
    }

    /// <summary>
    /// Factory class for supplying Entity Framework AOP proxies.
    /// </summary>
    public static partial class EfAOP
    {
        /// <summary>
        /// Returns AOP proxy for EF DbContext class that will instantiate DbContext and after usage calls DbContext.Dispose()
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DbContextSingleCallProxy<TDbContext> GetDbProxy<TDbContext>(IEnumerable<Aspect> aspects = null, bool lazyLoadingEnabled = true)
            where TDbContext : DbContext, new()
        {
            var proxy = new DbContextSingleCallProxy<TDbContext>(aspects, lazyLoadingEnabled);
            return proxy;
        }

        /// <summary>
        /// Returns AOP proxy for EF ObjectContext class
        /// </summary>
        /// <typeparam name="TObjectContext"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static ObjectContextSingleCallProxy<TObjectContext> GetOcProxy<TObjectContext>(IEnumerable<Aspect> aspects = null, bool lazyLoadingEnabled = true)
            where TObjectContext : ObjectContext, new()
        {
            var proxy = new ObjectContextSingleCallProxy<TObjectContext>(aspects, lazyLoadingEnabled);
            return proxy;
        }
    }

    public static partial class EfAopExts
    {
        /// <summary>
        /// Returns InstanceProxy[TDbContext] for DbContext instance that already exist.
        /// Returned proxy won't call DbContext.Dispose() after method invocation.
        /// Supports ExecuteCommand() method.
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="dbExistingContext"></param>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DbContextSingleCallProxy<TDbContext> GetDbProxy<TDbContext>(this TDbContext dbExistingContext, IEnumerable<Aspect> aspects = null)
            where TDbContext : DbContext, new()
        {
            var proxy = new DbContextSingleCallProxy<TDbContext>(dbExistingContext, aspects);
            return proxy;
        }

        /// <summary>
        /// Returns InstanceProxy[TObjectContext] for ObjectContext instance that already exist.
        /// Returned proxy won't call ObjectContext.Dispose() after method invocation.
        /// Supports ExecuteCommand() method.
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
