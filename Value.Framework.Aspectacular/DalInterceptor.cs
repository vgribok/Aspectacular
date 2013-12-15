using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular.Data
{
    /// <summary>
    /// Alloc/invoke/dispose convenience class for DalManager subclasses.
    /// </summary>
    /// <typeparam name="TMultiStoreMgr"></typeparam>
    public class DalSingleCallProxy<TMultiStoreMgr> : AllocateRunDisposeProxy<TMultiStoreMgr>, IEfCallInterceptor, IStorageCommandRunner<TMultiStoreMgr>
            where TMultiStoreMgr : DalManager, new()
    {
        public DalSingleCallProxy(IEnumerable<Aspect> aspects)
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

        #region Implementation of IStorageCommandRunner

        public int SaveChangeReturnValue
        {
            get { return this.AugmentedClassInstance.SaveChangeReturnValue; }
            set { this.AugmentedClassInstance.SaveChangeReturnValue = value; }
        }

        public int SaveChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        #endregion IStorageCommandRunner

        /// <summary>
        /// Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public int ExecuteCommand(Expression<Action<TMultiStoreMgr>> callExpression)
        {
            this.Invoke(callExpression);
            return this.SaveChangeReturnValue;
        }
    }
}

namespace Value.Framework.Aspectacular
{
    using Value.Framework.Aspectacular.Data;

    public static partial class AOP
    {
        /// <summary>
        /// Returns AOP proxy for a multi-data store DAL object.
        /// </summary>
        /// <typeparam name="TMultiStoreMgr"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DalSingleCallProxy<TMultiStoreMgr> GetDalProxy<TMultiStoreMgr>(IEnumerable<Aspect> aspects)
            where TMultiStoreMgr : DalManager, new()
        {
            var proxy = new DalSingleCallProxy<TMultiStoreMgr>(aspects);
            return proxy;
        }
    }
}