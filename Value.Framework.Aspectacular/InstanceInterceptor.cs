using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{

    public class InstanceInterceptor<TInstance> : Interceptor
        where TInstance : class
    {
        public new TInstance AugmentedClassInstance
        {
            get { return (TInstance)base.AugmentedClassInstance; }
        }

        public InstanceInterceptor(Func<TInstance> instanceFactory, Action<TInstance> instanceCleaner, Aspect[] aspects)
            : base(instanceFactory,
                   inst =>
                   {
                       if (instanceCleaner != null)
                           instanceCleaner((TInstance)inst);
                   },
                   aspects)
        {
        }

        public InstanceInterceptor(Func<TInstance> instanceFactory, params Aspect[] aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }

        public InstanceInterceptor(TInstance instance, params Aspect[] aspects)
            : this(() => instance, instanceCleaner: null, aspects: aspects)
        {
        }

        /// <summary>
        /// Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="callExpression"></param>
        /// <param name="retValPostProcessor">
        /// Delegate called immediately after callExpression function was executed. 
        /// Allows additional massaging of the returned value. Useful when LINQ suffix functions, like ToList(), Single(), etc. 
        /// need to be called in alloc/invoke/dispose pattern.
        /// </param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TInstance, TOut>> callExpression, Func<TOut, object> retValPostProcessor = null)
        {
            this.ResolveClassInstance();

            Func<TInstance, TOut> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                retVal = blDelegate.Invoke(this.AugmentedClassInstance);

                if (retValPostProcessor != null)
                    this.MethodExecutionResult = retValPostProcessor(retVal);
                else
                    this.MethodExecutionResult = retVal;
            });

            return retVal;
        }


        /// <summary>
        /// Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <param name="callExpression"></param>
        public void Invoke(Expression<Action<TInstance>> callExpression)
        {
            this.ResolveClassInstance();

            Action<TInstance> blDelegate = callExpression.Compile();
            this.InitMethodMetadata(callExpression, blDelegate);

            this.ExecuteMainSequence(() => blDelegate.Invoke(this.AugmentedClassInstance));
        }

        #region LINQ convenience shortcut methods

        /// <summary>
        /// Appends ToList() to IQueryable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.Invoke(linqQueryExpression, query => (query == null || query is IList<TEntity>) ? query as IList<TEntity> : query.ToList());
            IList<TEntity> entityList = (IList<TEntity>)this.MethodExecutionResult;
            return entityList;
        }

        /// <summary>
        /// Appends ToList() to IEnumerable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.Invoke(sequenceExpression, query => (query == null || query is IList<TEntity>) ? query as IList<TEntity> : query.ToList());
            IList<TEntity> entityList = (IList<TEntity>)this.MethodExecutionResult;
            return entityList;
        }

        /// <summary>
        /// Adds FirstOrDefault() to IQueryable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.Invoke(linqQueryExpression, query => query.FirstOrDefault());
            TEntity entity = (TEntity)this.MethodExecutionResult;
            return entity;
        }

        /// <summary>
        /// Adds FirstOrDefault() to IEnumerable
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.Invoke(sequenceExpression, query => query.FirstOrDefault());
            TEntity entity = (TEntity)this.MethodExecutionResult;
            return entity;
        }

        #endregion Linq convenience shortcut methods
    }

    /// <summary>
    /// Extensions and static convenience methods for intercepted method calls.
    /// </summary>
    public static partial class AOP
    {
        /// <summary>
        /// Executes/intercepts *static* function with TOut return result.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(Aspect[] aspects, Expression<Func<TOut>> callExpression)
        {
            var context = new Interceptor(null, aspects);
            TOut retVal = context.Invoke<TOut>(callExpression);
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return result.
        /// </summary>
        /// <param name="aspects"></param>
        /// <param name="callExpression"></param>
        public static void Invoke(Aspect[] aspects, Expression<Action> callExpression)
        {
            var context = new Interceptor(null, aspects);
            context.Invoke(callExpression);
        }

        public static InstanceInterceptor<TInstance> GetProxy<TInstance>(this TInstance instance, params Aspect[] aspects)
            where TInstance : class
        {
            var interceptor = new InstanceInterceptor<TInstance>(instance, aspects);
            return interceptor;
        }
    }
}
