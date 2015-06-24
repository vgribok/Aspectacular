#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Aspectacular
{
    /// <summary>
    /// AOP proxy for invoking non-static (instance) methods.
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    public class InstanceProxy<TInstance> : Proxy
        where TInstance : class
    {
        /// <summary>
        ///     Instance of an object whose methods are intercepted.
        ///     Null when static methods are intercepted.
        ///     Can be an derived from IAspect of object wants to be its own
        /// </summary>
        public new TInstance AugmentedClassInstance
        {
            get { return (TInstance)base.AugmentedClassInstance; }
        }

        protected InstanceProxy(Func<TInstance> instanceFactory, Action<TInstance> instanceCleaner, IEnumerable<Aspect> aspects)
            : base(instanceFactory,
                inst =>
                {
                    if(instanceCleaner != null)
                        instanceCleaner((TInstance)inst);
                },
                aspects)
        {
        }

        public InstanceProxy(Func<TInstance> instanceFactory, IEnumerable<Aspect> aspects)
            : this(instanceFactory, null, aspects)
        {
        }

        public InstanceProxy(TInstance instance, IEnumerable<Aspect> aspects)
            : this(() => instance, null, aspects)
        {
        }

        /// <summary>
        ///     Executes/intercepts *instance* function with TOut return value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="interceptedCallExpression"></param>
        /// <param name="retValPostProcessorExpression">
        ///     Delegate called immediately after callExpression function was executed.
        ///     Allows additional massaging of the returned value. Useful when LINQ suffix functions, like ToList(), Single(), etc.
        ///     need to be called in alloc/invoke/dispose pattern.
        /// </param>
        /// <returns></returns>
        public TOut Invoke<TOut>(Expression<Func<TInstance, TOut>> interceptedCallExpression, Expression<Func<TOut, object>> retValPostProcessorExpression = null)
        {
            this.ResolveClassInstance();

            Func<TInstance, TOut> blDelegate = interceptedCallExpression.Compile();
            Func<TOut, object> retValPostProcessor = retValPostProcessorExpression == null ? null : retValPostProcessorExpression.Compile();

            this.InitMethodMetadata(interceptedCallExpression, blDelegate, retValPostProcessorExpression);

            TOut retVal = default(TOut);

            this.ExecuteMainSequence(() =>
            {
                this.InvokeActualInterceptedMethod(() => retVal = blDelegate.Invoke(this.AugmentedClassInstance));
                this.CallReturnValuePostProcessor(retValPostProcessor, retVal);
            });

            return retVal;
        }

        /// <summary>
        ///     Executes/intercepts *instance* function with no return value.
        /// </summary>
        /// <param name="interceptedCallExpression"></param>
        public void Invoke(Expression<Action<TInstance>> interceptedCallExpression)
        {
            this.ResolveClassInstance();

            Action<TInstance> blDelegate = interceptedCallExpression.Compile();
            this.InitMethodMetadata(interceptedCallExpression, blDelegate, postProcessingMethodExpression: null);

            this.ExecuteMainSequence(() => this.InvokeActualInterceptedMethod(() => blDelegate.Invoke(this.AugmentedClassInstance)));
        }

        #region LINQ convenience shortcut methods

        private void LogLinqModifierName(NonEmptyString linqModifier, QueryModifiers queryModifiers)
        {
            Debug.Assert(!linqModifier.ToString().IsBlank());
            this.LogInformationData("LINQ Modifier", linqModifier);

            if(queryModifiers != null)
                this.LogInformationData("Query Modifiers", queryModifiers);
        }

        /// <summary>
        ///     Triggers query execution by appending ToList() to IQueryable, if returned result is not IList already.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <param name="queryModifiers">Optional filtering, sorting and paging applied to the query</param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("List<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers);
            this.Invoke(linqQueryExpression, query => query.ToIListWithMods(queryModifiers));
            IList<TEntity> entityList = (IList<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Appends ToList() to IEnumerable, if returned result is not IList already.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <param name="queryModifiers">Optional filtering, sorting and paging applied to the query</param>
        /// <returns></returns>
        public IList<TEntity> List<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("List<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers);
            this.Invoke(sequenceExpression, collection => collection.ToIListWithMods(queryModifiers));
            IList<TEntity> entityList = (IList<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Triggers query execution by appending ToList() to IQueryable, and returns List.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <param name="queryModifiers">Optional filtering, sorting and paging applied to the query</param>
        /// <returns></returns>
        public List<TEntity> ListList<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("ListList<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers);
            this.Invoke(linqQueryExpression, query => query.ToListWithMods(queryModifiers));
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Appends ToList() to IEnumerable.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <param name="queryModifiers">Optional filtering, sorting and paging applied to the query</param>
        /// <returns></returns>
        public List<TEntity> ListList<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("ListList<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers);
            this.Invoke(sequenceExpression, collection => collection.ToListWithMods(queryModifiers));
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Executes IQuerable that returns anonymous type.
        /// </summary>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public List<object> List(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("List(Expression<Func<TInstance, IQueryable>> linqQueryExpression)", queryModifiers: null);
            
            List<object> records = new List<object>();
            this.Invoke(linqQueryExpression, query => ToOjbectList(query, ref records));

            return records;
        }

        private static List<object> ToOjbectList(IEnumerable collection, ref List<object> records)
        {
            if(collection != null)
                collection.ForEach(records.Add);
            return records;
        }

        /// <summary>
        ///     Executes IEnumerable that returns anonymous type.
        /// </summary>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public List<object> List(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("List(Expression<Func<TInstance, IEnumerable>> sequenceExpression)", queryModifiers: null);
            
            List<object> records = new List<object>();
            this.Invoke(sequenceExpression, sequence => ToOjbectList(sequence, ref records));

            return records;
        }


        /// <summary>
        ///     Modifies "Select" query to bring only one page of data instead of an entire result set.
        ///     Executes modified query and returns a collection of records corresponding to the given page.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pageIndex">0-based data page index.</param>
        /// <param name="pageSize">Page size in the number of records.</param>
        /// <param name="linqQueryExpression">Query that returns entire set.</param>
        /// <returns>One page subset of data specified by the query.</returns>
        [Obsolete("Use List() with query modifiers instead")]
        public IList<TEntity> Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers: null);

            QueryModifiers mods = new QueryModifiers { Paging = new QueryModifiers.PagingInfo {PageIndex = pageIndex, PageSize = pageSize}};

            this.Invoke(linqQueryExpression, query => query.ToIListWithMods(mods));
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Filters in on-page subset of the collection.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="pageIndex">0-based data page index.</param>
        /// <param name="pageSize">Page size in the number of records.</param>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        [Obsolete("Use List() with query modifiers instead")]
        public IList<TEntity> Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Page<TEntity>(int pageIndex, int pageSize, Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers: null);

            QueryModifiers mods = new QueryModifiers { Paging = new QueryModifiers.PagingInfo { PageIndex = pageIndex, PageSize = pageSize } };

            this.Invoke(sequenceExpression, sequence => sequence.ToListWithMods(mods));
            List<TEntity> entityList = (List<TEntity>)this.ReturnedValue;
            return entityList;
        }

        /// <summary>
        ///     Adds FirstOrDefault() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Single<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers: null);
            this.Invoke(linqQueryExpression, query => FirstOrDefault(query)); 
            TEntity entity = (TEntity)this.ReturnedValue;
            return entity;
        }

        private static object FirstOrDefault<TEntity>(IQueryable<TEntity> query)
        {
            return query.FirstOrDefault();
        }

        private static object FirstOrDefault<TEntity>(IEnumerable<TEntity> query)
        {
            return query.FirstOrDefault();
        }

        /// <summary>
        ///     Adds FirstOrDefault() to IEnumerable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public TEntity Single<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Single<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers: null);
            this.Invoke(sequenceExpression, collection => FirstOrDefault(collection));
            TEntity entity = (TEntity)this.ReturnedValue;
            return entity;
        }

        /// <summary>
        ///     Executes anonymous IQueryable and return first object or null.
        /// </summary>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public object Single(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("Single(Expression<Func<TInstance, IQueryable>> linqQueryExpression)", queryModifiers: null);

            object entity = null;
            this.Invoke(linqQueryExpression, query => GetFirst(query, out entity)); 
            return entity;
        }

        private static object GetFirst(IEnumerable collection, out object obj)
        {
            obj = collection == null ? null : collection.Cast<object>().FirstOrDefault();
            return obj;
        }

        /// <summary>
        ///     Returns first anonymous object from IEnumerable, or null.
        /// </summary>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public object Single(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("Single(Expression<Func<TInstance, IEnumerable>> sequenceExpression)", queryModifiers: null);

            object entity = null;
            this.Invoke(sequenceExpression, collection => GetFirst(collection, out entity));
            return entity;
        }


        /// <summary>
        /// Can't use QueryModifiersExtensions.LongCount()
        /// because that returns int, and we need object.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="queryModifiers"></param>
        /// <returns></returns>
        private static object LongCountInternal<TEntity>(IQueryable<TEntity> query, QueryModifiers queryModifiers)
        {
            return query.LongCount(queryModifiers);
        }

        private static object LongCountInternal<TEntity>(IEnumerable<TEntity> collection, QueryModifiers queryModifiers)
        {
            return collection.LongCount(queryModifiers);
        }

        /// <summary>
        ///     Adds LongCount() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <param name="queryModifiers"></param>
        /// <returns>Number of records that would be returned by the query</returns>
        public long Count<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("Count<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers);
            this.Invoke(linqQueryExpression, query => LongCountInternal(query, queryModifiers));
            long count = (long)this.ReturnedValue;
            return count;
        }

        /// <summary>
        ///     Adds LongCount() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <param name="queryModifiers"></param>
        /// <returns>Number of items in the collection</returns>
        public long Count<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression, QueryModifiers queryModifiers = null)
        {
            this.LogLinqModifierName("Count<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers);
            this.Invoke(sequenceExpression, collection => LongCountInternal(collection, queryModifiers)); 
            long count = (long)this.ReturnedValue;
            return count;
        }


        private static object ExistsInternal<TEntity>(IQueryable<TEntity> query)
        {
            return query.Exists();
        }

        /// <summary>
        ///     Adds Exists() to IQueryable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="linqQueryExpression"></param>
        /// <returns></returns>
        public bool Exists<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)
        {
            this.LogLinqModifierName("Exists<TEntity>(Expression<Func<TInstance, IQueryable<TEntity>>> linqQueryExpression)", queryModifiers: null);
            this.Invoke(linqQueryExpression, query => ExistsInternal(query));
            return (bool)this.ReturnedValue;
        }

        /// <summary>
        ///     Adds Any() to IEnumerable return result.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sequenceExpression"></param>
        /// <returns></returns>
        public bool Exists<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)
        {
            this.LogLinqModifierName("Exists<TEntity>(Expression<Func<TInstance, IEnumerable<TEntity>>> sequenceExpression)", queryModifiers: null);
            this.Invoke(sequenceExpression, collection => collection.Any());
            return (bool)this.ReturnedValue;
        }

        public bool Exists(Expression<Func<TInstance, IQueryable>> linqQueryExpression)
        {
            this.LogLinqModifierName("Exists(Expression<Func<TInstance, IQueryable>> linqQueryExpression)", queryModifiers: null);
            object record = this.Single(linqQueryExpression);
            return record != null;
        }

        public bool Exists(Expression<Func<TInstance, IEnumerable>> sequenceExpression)
        {
            this.LogLinqModifierName("Exists(Expression<Func<TInstance, IEnumerable>> sequenceExpression)", queryModifiers: null);
            object record = this.Single(sequenceExpression);
            return record != null;
        }

        #endregion Linq convenience shortcut methods
    }

    /// <summary>
    ///     Extensions and static convenience methods for intercepted method calls.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static partial class AOP
    {
        /// <summary>
        ///     Executes/intercepts *static* function with TOut return result.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="interceptedCallExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(IEnumerable<Aspect> aspects, Expression<Func<TOut>> interceptedCallExpression)
        {
            var proxy = GetProxy(aspects);
            TOut retVal = proxy.Invoke(interceptedCallExpression);
            return retVal;
        }

        /// <summary>
        /// Returns AOP Proxy that can be used to Invoke() static methods.
        /// Consider AOP.Invoke() as an alternative.
        /// </summary>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static Proxy GetProxy(IEnumerable<Aspect> aspects)
        {
            return new Proxy(instanceFactory: null, aspects: aspects);
        }

        /// <summary>
        /// Returns AOP Proxy that can be used to Invoke() static methods.
        /// Consider AOP.Invoke() as an alternative.
        /// </summary>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static Proxy GetProxy(params Aspect[] aspects)
        {
            return GetProxy(aspects.AsEnumerable());
        }

        /// <summary>
        ///     Executes/intercepts *static* function with TOut return result, using only default (.config) aspects.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="interceptedCallExpression"></param>
        /// <returns></returns>
        public static TOut Invoke<TOut>(Expression<Func<TOut>> interceptedCallExpression)
        {
            return Invoke(null, interceptedCallExpression);
        }

        /// <summary>
        ///     Executes/intercepts *static* function with no return result.
        /// </summary>
        /// <param name="aspects"></param>
        /// <param name="interceptedCallExpression"></param>
        public static void Invoke(IEnumerable<Aspect> aspects, Expression<Action> interceptedCallExpression)
        {
            var proxy = GetProxy(aspects);
            proxy.Invoke(interceptedCallExpression);
        }

        /// <summary>
        ///     Executes/intercepts *static* function with no return result, using only default (.config) aspects.
        /// </summary>
        /// <param name="interceptedCallExpression"></param>
        public static void Invoke(Expression<Action> interceptedCallExpression)
        {
            Invoke(null, interceptedCallExpression);
        }


        /// <summary>
        /// Returns AOP-enabled service interface previously registered using SvcLocator.Register().
        /// </summary>
        /// <typeparam name="TInterface">An interface type.</typeparam>
        /// <param name="aspects"></param>
        /// <returns>AOP proxy representing service interface.</returns>
        public static InstanceProxy<TInterface> GetService<TInterface>(IEnumerable<Aspect> aspects = null)
            where TInterface : class
        {
            InstanceProxy<TInterface> proxy = new InstanceProxy<TInterface>(SvcLocator.Get<TInterface>(), aspects);
            return proxy;
        }
    }

    public static partial class AopExsts
    {
        /// <summary>
        ///     Retrieves AOP-augmented proxy, with specified set of aspects attached, for any given object referenced by instance
        ///     parameter.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static InstanceProxy<TInstance> GetProxy<TInstance>(this TInstance instance, IEnumerable<Aspect> aspects = null)
            where TInstance : class
        {
            var proxy = new InstanceProxy<TInstance>(instance, aspects);
            return proxy;
        }
    }
}