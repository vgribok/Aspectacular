using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Aspectacular.Data;

namespace Value.Framework.Aspectacular.EntityFramework
{
    public class DbContextSingleCallInterceptor<TDbContext> : AllocateRunDisposeInterceptor<TDbContext>, IEfCallInterceptor, IStorageCommandRunner<TDbContext>
        where TDbContext : DbContext, new()
    {
        public DbContextSingleCallInterceptor(params Aspect[] aspects)
            : base(aspects)
        {
        }

        #region Base class overrides

        protected override void InvokeActualInterceptedMethod(Action interceptedMethodClosure)
        {
            base.InvokeActualInterceptedMethod(interceptedMethodClosure);
            this.DoSaveChanges();
        }

        #endregion Base class overrides

        #region Implementation of IEfCallInterceptor

        public int SaveChangeReturnValue { get; set; }

        public int SaveChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        #endregion Implementation of IEfCallInterceptor

        #region Implementation of IStorageCommandRunner

        /// <summary>
        /// Command that returns scalar value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public TOut ExecuteCommand<TOut>(Expression<Func<TDbContext, TOut>> callExpression)
        {
            return this.Invoke(callExpression);
        }

        /// <summary>
        /// Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public int ExecuteCommand(Expression<Func<TDbContext>> callExpression)
        {
            this.Invoke(callExpression);
            return this.SaveChangeReturnValue;
        }

        #endregion IStorageCommandRunner
    }

    public class ObjectContextSingleCallInterceptor<TObjectContext> : AllocateRunDisposeInterceptor<TObjectContext>, IEfCallInterceptor, IStorageCommandRunner<TObjectContext>
        where TObjectContext : ObjectContext, new()
    {
        public ObjectContextSingleCallInterceptor(params Aspect[] aspects)
            : base(aspects)
        {
        }

        #region Base class overrides

        protected override void InvokeActualInterceptedMethod(Action interceptedMethodClosure)
        {
            base.InvokeActualInterceptedMethod(interceptedMethodClosure);
            this.DoSaveChanges();
        }

        #endregion Base class overrides

        #region Implementation of IEfCallInterceptor

        public int SaveChangeReturnValue { get; set; }

        public int SaveChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        #endregion Implementation of IEfCallInterceptor

        #region Implementation of IStorageCommandRunner

        /// <summary>
        /// Command that returns scalar value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public TOut ExecuteCommand<TOut>(Expression<Func<TObjectContext, TOut>> callExpression)
        {
            return this.Invoke(callExpression);
        }

        /// <summary>
        /// Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public int ExecuteCommand(Expression<Func<TObjectContext>> callExpression)
        {
            this.Invoke(callExpression);
            return this.SaveChangeReturnValue;
        }

        #endregion IStorageCommandRunner
    }

    /// <summary>
    /// Factory class for supplying Entity Framework AOP proxies.
    /// </summary>
    public static partial class EfAOP
    {
        /// <summary>
        /// Returns AOP proxy for EF DbContext class
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DbContextSingleCallInterceptor<TDbContext> GetDbProxy<TDbContext>(params Aspect[] aspects)
            where TDbContext : DbContext, new()
        {
            var proxy = new DbContextSingleCallInterceptor<TDbContext>(aspects);
            return proxy;
        }

        /// <summary>
        /// Returns AOP proxy for EF ObjectContext class
        /// </summary>
        /// <typeparam name="TObjectContext"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static ObjectContextSingleCallInterceptor<TObjectContext> GetOcProxy<TObjectContext>(params Aspect[] aspects)
            where TObjectContext : ObjectContext, new()
        {
            var proxy = new ObjectContextSingleCallInterceptor<TObjectContext>(aspects);
            return proxy;
        }
    }
}
