using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Defines common query modifiers
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IQueryModifier<TEntity>
    {
        /// <summary>
        /// Modifies given query in some way.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        IQueryable<TEntity> Augment(IQueryable<TEntity> query);

        IEnumerable<TEntity> Augment(IEnumerable<TEntity> query);
    }

    public static class QueryModifiersExtensions
    {
        /// <summary>
        /// Modifies a query by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query">Query to modify.</param>
        /// <param name="optionalQueryModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        public static IQueryable<TEntity> AugmentQuery<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if(optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
                return query;

            return query == null ? null : optionalQueryModifiers.Where(mod => mod != null).Aggregate(query, (current, modifier) => modifier.Augment(current));
        }
        /// <summary>
        /// Modifies a collection by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection">Query to modify.</param>
        /// <param name="optionalQueryModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> AugmentQuery<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if (optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
                return collection;

            return collection == null ? null : optionalQueryModifiers.Where(mod => mod != null).Aggregate(collection, (current, modifier) => modifier.Augment(current));
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            var newQuery = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static TEntity FirstOrDefaultWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newQuery as TEntity ?? newQuery.FirstOrDefault();

            return retVal;
        }

        public static TEntity FirstOrDefaultWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newCollection = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newCollection as TEntity ?? newCollection.FirstOrDefault();

            return retVal;
        }

        public static TEntity SingleOrDefaultWithMods<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newQuery = query.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newQuery as TEntity ?? newQuery.SingleOrDefault();

            return retVal;
        }

        public static TEntity SingleOrDefaultWithMods<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers) where TEntity : class
        {
            var newCollection = collection.AugmentQuery(optionalQueryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            TEntity retVal = newCollection as TEntity ?? newCollection.SingleOrDefault();

            return retVal;
        }
    }

    #region Stock Query Modifier Classes

    /// <summary>
    /// Defines query paging modifier. 
    /// Modified query will bring only a subset of data 
    /// not exceeding in size the number of records specified by PageSize property.
    /// </summary>
    public class Paging<TEntity> : IQueryModifier<TEntity>
    {
        public Paging(int pageIndex, int pageSize)
        {
            if(pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex cannot be negative");
            if(pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize cannot less than 1");

            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
        }

        public Paging() : this(pageIndex: 0, pageSize: 20)
        {
        }

        /// <summary>
        /// Zero-based page index;
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// Data page size in the number of records.
        /// </summary>
        public int PageSize { get; set; }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            int skipCount = this.CalcSkip();
            return query.Skip(skipCount).Take(this.PageSize);
        }

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> query)
        {
            int skipCount = this.CalcSkip();
            return query.Skip(skipCount).Take(this.PageSize);
        }

        private int CalcSkip()
        {
            if (this.PageIndex < 0)
                throw new ArgumentException("PageIndex parameter cannot be negative.");
            if (this.PageSize < 1)
                throw new ArgumentException("PageSize parameter must be greater than 0.");

            return this.PageIndex * this.PageSize;
        }
    }

    public abstract class MemberAugmentBase<TEntity, TKey> : IQueryModifier<TEntity>
    {
        public Expression<Func<TEntity, TKey>> EntitySortProperty { get; set; }

        protected MemberAugmentBase(Expression<Func<TEntity, TKey>> sortProperty)
        {
            this.EntitySortProperty = sortProperty;
        }

        protected MemberAugmentBase()
            : this(sortProperty: null)
        {
        }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            if (query == null)
                return null;

            query = this.Augment(query, this.EntitySortProperty);

            return query;
        }

        protected abstract IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> expression);

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> collection)
        {
            if (collection == null)
                return null;

            collection = this.AugmentCollection(collection, this.EntitySortProperty.Compile());

            return collection;
        }

        protected abstract IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> func);
    }

    public class SortingAsc<TEntity, TKey> : MemberAugmentBase<TEntity, TKey>
    {
        public SortingAsc(Expression<Func<TEntity, TKey>> sortProperty) : base(sortProperty)
        {
        }

        public SortingAsc() : base()
        {
        }

        protected override IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> sortPropertyExpression)
        {
            return query.OrderBy(sortPropertyExpression);
        }

        protected override IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> sortFunc)
        {
            return collection.OrderBy(sortFunc);
        }
    }

    public class SortingDesc<TEntity, TKey> : MemberAugmentBase<TEntity, TKey>
    {
        public SortingDesc(Expression<Func<TEntity, TKey>> sortProperty)
            : base(sortProperty)
        {
        }

        public SortingDesc()
            : base()
        {
        }

        protected override IQueryable<TEntity> Augment(IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> sortPropertyExpression)
        {
            return query.OrderByDescending(sortPropertyExpression);
        }

        protected override IEnumerable<TEntity> AugmentCollection(IEnumerable<TEntity> collection, Func<TEntity, TKey> sortFunc)
        {
            return collection.OrderByDescending(sortFunc);
        }
    }

    #endregion Stock Query Modifier Classes
}
