using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public class InstanceProxy<TInstance> : Proxy
        where TInstance : class
    {
        public new TInstance AugmentedClassInstance
        {
            get { return (TInstance)base.AugmentedClassInstance; }
        }

        public InstanceProxy(Func<TInstance> instanceFactory, Action<TInstance> instanceCleaner, IEnumerable<Aspect> aspects)
            : base(instanceFactory,
                   inst =>
                   {
                       if (instanceCleaner != null)
                           instanceCleaner((TInstance)inst);
                   },
                   aspects)
        {
        }

        public InstanceProxy(Func<TInstance> instanceFactory, IEnumerable<Aspect> aspects)
            : this(instanceFactory, instanceCleaner: null, aspects: aspects)
        {
        }

        public InstanceProxy(TInstance instance, IEnumerable<Aspect> aspects)
            : this(() => instance, instanceCleaner: null, aspects: aspects)
        {
        }

        /// <summary>
        /// Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="interceptedCallExpression"></param>
        /// <param name="retValPostProcessor">
        /// Delegate called immediately after callExpression function was executed. 
        /// Allows additional massaging of the returned value. Useful when LINQ suffix functions, like ToList(), Single(), etc. 
        /// need to be called in alloc/invoke/dispose pattern.
        /// </param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TInstance, TOut>> interceptedCallExpression, Func<TOut, object> retValPostProcessor = null)
        {
            this.ResolveClassInstance();

            Func<TInstance, TOut> blDelegate = interceptedCallExpression.Compile();
            this.InitMethodMetadata(interceptedCallExpression, blDelegate);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                this.InvokeActualInterceptedMethod(() => retVal = blDelegate.Invoke(this.AugmentedClassInstance));
                this.CallReturnValuePostProcessor<TOut>(retValPostProcessor, retVal);
            });

            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <param name="interceptedCallExpression"></param>
        public void Invoke(Expression<Action<TInstance>> interceptedCallExpression)
        {
            this.ResolveClassInstance();

            Action<TInstance> blDelegate = interceptedCallExpression.Compile();
            this.InitMethodMetadata(interceptedCallExpression, blDelegate);

            this.ExecuteMainSequence(() => this.InvokeActualInterceptedMethod(() => blDelegate.Invoke(this.AugmentedClassInstance)));
        }

        #region LINQ convenience shortcut methods

        private void LogLinqModifierName(NonEmptyString linqModifier)
        {
            Debug.Assert(!linqModifier.ToString().IsBlank());
            this.LogInformationData("LINQ Modifier", linqModifier);
        }

        /// <summary>
        /// Triggers query execution by appending ToList() to IQueryable, if returned result is not IList already.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("List<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)");
            this.Invoke(linqQueryExpression, query => (query == null || query is IList<TEntity>) ? query as IList<TEntity> : query.ToList());
            IList<TEntity> entityList = (IList<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Appends ToList() to IEnumerable, if returned result is not IList already.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("List<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)");
            this.Invoke(sequenceExpression, sequence => (sequence == null || sequence is IList<TEntity>) ? sequence as IList<TEntity> : sequence.ToList());
            IList<TEntity> entityList = (IList<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Triggers query execution by appending ToList() to IQueryable, and returns List.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public List<TEntity> ListList<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("ListList<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)");
            this.Invoke(linqQueryExpression, query => query.ToList());
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Appends ToList() to IEnumerable.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public List<TEntity> ListList<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("ListList<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)");
            this.Invoke(sequenceExpression, sequence => sequence.ToList());
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Executes IQuerable that returns anonymous type.
        /// </summary>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public List<object> List(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("List(Expression<Func<TInstance, IQueryable>> linqQueryExpression)");
            List<object> records = new List<object>();

            this.Invoke(linqQueryExpression, query => 
                {
                    query.ToEnumerable().ForEach(record => records.Add(record));
                    return records;
                });

            return records;
        }

        /// <summary>
        /// Executes IEnumerable that returns anonymous type.
        /// </summary>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public List<object> List(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("List(Expression<Func<TInstance, IEnumerable>> sequenceExpression)");
            List<object> records = new List<object>();

            this.Invoke(sequenceExpression, sequence =>
            {
                sequence.ForEach(record => records.Add(record));
                return records;
            });

            return records;
        }


        private static int CalcSkip(int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
                throw new ArgumentException("pageIndex parameter cannot be negative.");
            if (pageSize < 1)
                throw new ArgumentException("pageSize parameter must be greater than 0.");

            return pageIndex * pageSize;
        }

        /// <summary>
        /// Modifies "Select" query to bring only one page of data instead of an entire result set.
        /// Executes modified query and returns a collection of records corresponding to the given page.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pageIndex">0-based data page index.</param>
        /// <param name="pageSize">Page size in the number of records.</param>
        /// <param name="linqQueryExpression">Query that returns entire set.</param>
        /// <returns>One page subset of data specified by the query.</returns>
        public IList<TEntity> Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)");

            int skipCount = CalcSkip(pageIndex, pageIndex);

            this.Invoke(linqQueryExpression, query => (query == null) ? null : query.Skip(skipCount).Take(pageSize).ToList());
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Filters in on-page subset of the collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pageIndex">0-based data page index.</param>
        /// <param name="pageSize">Page size in the number of records.</param>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public IList<TEntity> Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)");

            int skipCount = CalcSkip(pageIndex, pageIndex);

            this.Invoke(sequenceExpression, query => (query == null) ? null : query.Skip(skipCount).Take(pageSize).ToList());
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        /// Adds FirstOrDefault() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Single<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)");
            this.Invoke(linqQueryExpression, query => query.FirstOrDefault());
            TEntity entity = (TEntity)this.ReturnedValue;
            return entity;
        }

        /// <summary>
        /// Adds FirstOrDefault() to IEnumerable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Single<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)");
            this.Invoke(sequenceExpression, sequence => sequence.FirstOrDefault());
            TEntity entity = (TEntity)this.ReturnedValue;
            return entity;
        }

        /// <summary>
        /// Executes anonymous IQueryable and return first object or null.
        /// </summary>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public object Single(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("Single(Expression<Func<TInstance, IQueryable>> linqQueryExpression)");

            object entity = null;

            this.Invoke(linqQueryExpression, query =>
            {
                foreach (object record in query)
                {
                    entity = record;
                    return record;
                }
                return null;
            });

            return entity;
        }

        /// <summary>
        /// Returns first anonymous object from IEnumerable, or null.
        /// </summary>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public object Single(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("Single(Expression<Func<TInstance, IEnumerable>> sequenceExpression)");

            object entity = null;

            this.Invoke(sequenceExpression, sequence =>
            {
                foreach (object record in sequence)
                {
                    entity = record;
                    return record;
                }
                return null;
            });

            return entity;
        }

        /// <summary>
        /// Adds Exists() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public bool Exists<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Exists<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)");
            this.Invoke(linqQueryExpression, query => query.Exists());
            return (bool)this.ReturnedValue;
        }

        /// <summary>
        /// Adds Any() to IEnumerable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public bool Exists<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Exists<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)");
            this.Invoke(sequenceExpression, sequence => sequence.Any());
            return (bool)this.ReturnedValue;
        }

        public bool Exists(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("Exists(Expression<Func<TInstance, IQueryable>> linqQueryExpression)");
            object record = this.Single(linqQueryExpression);
            return record != null;
        }

        public bool Exists(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("Exists(Expression<Func<TInstance, IEnumerable>> sequenceExpression)");
            object record = this.Single(sequenceExpression);
            return record != null;
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
        /// <param name="interceptedCallExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(IEnumerable<Aspect> aspects, Expression<Func<TOut>> interceptedCallExpression)
        {
            var context = new Proxy(null, aspects);
            TOut retVal = context.Invoke<TOut>(interceptedCallExpression);
            return retVal;
        }

        /// <summary>
        /// Executes/intercepts *static* function with TOut return result, using only default (.config) aspects.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="interceptedCallExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(Expression<Func<TOut>> interceptedCallExpression)
        {
            return Invoke<TOut>(aspects: null, interceptedCallExpression: interceptedCallExpression);
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return result.
        /// </summary>
        /// <param name="aspects"></param>
        /// <param name="interceptedCallExpression"></param>
        public static void Invoke(IEnumerable<Aspect> aspects, Expression<Action> interceptedCallExpression)
        {
            var context = new Proxy(null, aspects);
            context.Invoke(interceptedCallExpression);
        }

        /// <summary>
        /// Executes/intercepts *static* function with no return result, using only default (.config) aspects.
        /// </summary>
        /// <param name="interceptedCallExpression"></param>
        public static void Invoke(Expression<Action> interceptedCallExpression)
        {
            Invoke(aspects: null, interceptedCallExpression: interceptedCallExpression);
        }

        /// <summary>
        /// Retrieves AOP-augmented proxy, with specified set of aspects attached, for any given object referenced by instance parameter.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static InstanceProxy<TInstance> GetProxy<TInstance>(this TInstance instance, IEnumerable<Aspect> aspects = null)
            where TInstance : class
        {
            var interceptor = new InstanceProxy<TInstance>(instance, aspects);
            return interceptor;
        }
    }
}
