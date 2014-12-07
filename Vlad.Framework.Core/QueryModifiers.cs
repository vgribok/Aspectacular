﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace Aspectacular
{
    /// <summary>
    /// Defines common query modifiers: Filtering, Sorting and Paging.
    /// It is web-service-friendly and XML & JSON-serializable.
    /// </summary>
    public class QueryModifiers
    {
        #region Inner structures

        public class PagingInfo
        {
            public int PageIndex { get; set; }
            public int PageSize { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                return new Paging<TEntity>(this.PageIndex, this.PageSize);
            }

            /// <summary>
            /// Serves as a hash function for a particular type. 
            /// </summary>
            /// <returns>
            /// A hash code for the current <see cref="T:System.Object"/>.
            /// </returns>
            public override int GetHashCode()
            {
                return this.PageIndex.GetHashCode() ^ this.PageSize.GetHashCode();
            }
        }

        public class FilterInfo
        {
            public string FilterColumnName { get; set; }
            public object FilterValue { get; set; }
            public DynamicFilterOperator FilterOperator { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                Expression<Func<TEntity, bool>> predicate = PredicateBuilder.GetFilterPredicate<TEntity>(this.FilterColumnName, this.FilterOperator, this.FilterValue);
                QueryFilter<TEntity> filter = new QueryFilter<TEntity>(predicate);
                return filter;
            }

            /// <summary>
            /// Serves as a hash function for a particular type. 
            /// </summary>
            /// <returns>
            /// A hash code for the current <see cref="T:System.Object"/>.
            /// </returns>
            public override int GetHashCode()
            {
                int[] hashCodes = new int[3];
                hashCodes[0] = this.FilterColumnName == null ? 0 : this.FilterColumnName.GetHashCode();
                hashCodes[1] = this.FilterValue == null ? 0 : this.FilterValue.GetHashCode();
                hashCodes[2] = this.FilterOperator.GetHashCode();

                return hashCodes.Aggregate((a, b) => a ^ b);
            }
        }
        // ReSharper restore PossiblyMistakenUseOfParamsMethod

        public enum SortOrder
        {
            Ascending, Descending
        }

        public class SortingInfo
        {
            public SortOrder SortOrder { get; set; }
            public string SortFieldName { get; set; }

            internal IQueryModifier<TEntity> GetModifier<TEntity>()
            {
                var mod = new Sorter<TEntity>(this.SortFieldName, this.SortOrder == SortOrder.Ascending);
                return mod;
            }

            /// <summary>
            /// Serves as a hash function for a particular type. 
            /// </summary>
            /// <returns>
            /// A hash code for the current <see cref="T:System.Object"/>.
            /// </returns>
            public override int GetHashCode()
            {
                int sortFieldHashCode = this.SortFieldName == null ? 0 : this.SortFieldName.GetHashCode();
                return this.SortOrder.GetHashCode() ^ sortFieldHashCode;
            }
        }

        #endregion Inner structures

        /// <summary>
        /// Query column value filters combined by "and" operator.
        /// Can be null if filtering is not necessary.
        /// </summary>
        public List<FilterInfo> Filters { get; set; }

        /// <summary>
        /// Query sorting criteria supporting multiple columns, 
        /// with ascending and descending sorting.
        /// Can be null if sorting is not necessary.
        /// </summary>
        public List<SortingInfo> Sorting { get; set; }

        /// <summary>
        /// Data page information.
        /// Can be null if entire result set should be returned.
        /// </summary>
        public PagingInfo Paging { get; set; }

        #region Utility methods

        /// <summary>
        /// A shortcut method to add sorting.
        /// </summary>
        /// <param name="cortColumnName"></param>
        /// <param name="sortOrder"></param>
        /// <returns></returns>
        public QueryModifiers AddSortCriteria(string cortColumnName, SortOrder sortOrder = SortOrder.Ascending)
        {
            if(this.Sorting == null)
                this.Sorting = new List<SortingInfo>();

            this.Sorting.Add(new SortingInfo { SortFieldName = cortColumnName, SortOrder = sortOrder });

            return this;
        }

        /// <summary>
        /// A shortcut methods to add a filter.
        /// </summary>
        /// <param name="filterColumnName"></param>
        /// <param name="filterOperator"></param>
        /// <param name="filterValue"></param>
        /// <returns></returns>
        public QueryModifiers AddFilter(string filterColumnName, DynamicFilterOperator filterOperator, object filterValue)
        {
            if(this.Filters == null)
                this.Filters = new List<FilterInfo>();

            this.Filters.Add(new FilterInfo { FilterColumnName = filterColumnName, FilterOperator = filterOperator, FilterValue = filterValue });

            return this;
        }

        /// <summary>
        /// A shortcut method to add paging.
        /// </summary>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size in item number.</param>
        /// <returns></returns>
        public QueryModifiers AddPaging(int pageIndex, int pageSize)
        {
            this.Paging = new PagingInfo
            {
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return this;
        }

        #endregion Utility methods

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            int[] hashCodes = { 0, 0, 0 };
            
            if(this.Filters != null)
                hashCodes[0] = this.Filters.Select(f => f.GetHashCode()).Aggregate((a, b) => a ^ b);

            if(this.Sorting != null)
                hashCodes[1] = this.Sorting.Select(s => s.GetHashCode()).Aggregate((a, b) => a ^ b);

            if(this.Paging != null)
                hashCodes[2] = this.Paging.GetHashCode();

            if (hashCodes.Sum() == 0)
                // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
                return base.GetHashCode();

            int hashCode = hashCodes.Aggregate((a, b) => a ^ b);
            return hashCode;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            string json = new JavaScriptSerializer().Serialize(this);
            return json;
        }
    }

    /// <summary>
    /// Defines common query modifiers
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    internal interface IQueryModifier<TEntity>
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
        public static IQueryable<TEntity> Augment<TEntity>(this IQueryable<TEntity> query, QueryModifiers queryModifiers)
        {
            IQueryModifier<TEntity>[] mods = queryModifiers.GetModifiers<TEntity>();
            return query.AugmentQuery(mods);
        }

        public static IEnumerable<TEntity> Augment<TEntity>(this IEnumerable<TEntity> collection, QueryModifiers queryModifiers)
        {
            IQueryModifier<TEntity>[] mods = queryModifiers.GetModifiers<TEntity>();
            return collection.AugmentCollection(mods);
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IQueryable<TEntity> query, QueryModifiers queryModifiers)
        {
            var newQuery = query.Augment(queryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IQueryable<TEntity> query, QueryModifiers queryModifiers)
        {
            var newQuery = query.Augment(queryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static IList<TEntity> ToIListWithMods<TEntity>(this IEnumerable<TEntity> collection, QueryModifiers queryModifiers)
        {
            var newQuery = collection.Augment(queryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IList<TEntity> retVal = newQuery as IList<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static List<TEntity> ToListWithMods<TEntity>(this IEnumerable<TEntity> collection, QueryModifiers queryModifiers)
        {
            var newQuery = collection.Augment(queryModifiers);

            // ReSharper disable once SuspiciousTypeConversion.Global
            List<TEntity> retVal = newQuery as List<TEntity> ?? newQuery.ToList();

            return retVal;
        }

        public static long LongCount<TEntity>(this IQueryable<TEntity> query, QueryModifiers queryModifiers)
        {
            var newQuery = query.Augment(queryModifiers);
            long count = newQuery.Count();
            return count;
        }

        public static long LongCount<TEntity>(this IEnumerable<TEntity> collection, QueryModifiers queryModifiers)
        {
            var newQuery = collection.Augment(queryModifiers);
            long count = newQuery.Count();
            return count;
        }

        #region Internal methods

        /// <summary>
        /// Modifies a query by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query">Query to modify.</param>
        /// <param name="optionalQueryModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        internal static IQueryable<TEntity> AugmentQuery<TEntity>(this IQueryable<TEntity> query, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if (optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
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
        internal static IEnumerable<TEntity> AugmentCollection<TEntity>(this IEnumerable<TEntity> collection, params IQueryModifier<TEntity>[] optionalQueryModifiers)
        {
            if (optionalQueryModifiers == null || optionalQueryModifiers.Length == 0)
                return collection;

            return collection == null ? null : optionalQueryModifiers.Where(mod => mod != null).Aggregate(collection, (current, modifier) => modifier.Augment(current));
        }

        #endregion Internal methods
    }

    #region Stock Query Modifier Classes

    /// <summary>
    /// Defines query paging modifier. 
    /// Modified query will bring only a subset of data 
    /// not exceeding in size the number of records specified by PageSize property.
    /// </summary>
    internal class Paging<TEntity> : IQueryModifier<TEntity>
    {
        internal Paging(int pageIndex, int pageSize)
        {
            if(pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex cannot be negative");
            if(pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize cannot less than 1");

            this.PageIndex = pageIndex;
            this.PageSize = pageSize;
        }

        internal Paging() : this(pageIndex: 0, pageSize: 20)
        {
        }

        /// <summary>
        /// Zero-based page index;
        /// </summary>
        internal int PageIndex { get; set; }

        /// <summary>
        /// Data page size in the number of records.
        /// </summary>
        internal int PageSize { get; set; }

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

    /// <summary>
    /// Allows using List(), Single() and other AOP wire-tripping functions 
    /// to apply query augmentation to DAL methods returning IQueryable.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    internal class QueryFilter<TEntity> : IQueryModifier<TEntity>
    {
        internal Expression<Func<TEntity, bool>> QueryablePredicate { get; set; }
        internal Func<TEntity, bool> EnumerablePredicate { get; set; }

        internal QueryFilter(Expression<Func<TEntity, bool>> predicate)
        {
            this.QueryablePredicate = predicate;
        }

        internal QueryFilter(Func<TEntity, bool> predicate)
        {
            this.EnumerablePredicate = predicate;
        }

        internal QueryFilter()
        {
        }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            return query.Where(this.QueryablePredicate);
        }

        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> query)
        {
            if(this.EnumerablePredicate == null && this.QueryablePredicate != null)
                this.EnumerablePredicate = this.QueryablePredicate.Compile();

            if (this.EnumerablePredicate == null)
                throw new NullReferenceException("EnumerablePredicate must be specified.");

            return query.Where(this.EnumerablePredicate);
        }
    }

    internal class Sorter<TEntity> : IQueryModifier<TEntity>
    {
        internal Sorter(NonEmptyString propertyName, bool isAscending)
        {
            if(propertyName == null)
                throw new ArgumentNullException("propertyName");
            
            this.PropertyName = propertyName;
            this.IsAscending = isAscending;
        }

        public string PropertyName { get; set; }

        public bool IsAscending { get; set; }

        IQueryable<TEntity> IQueryModifier<TEntity>.Augment(IQueryable<TEntity> query)
        {
            Type propType = this.GetPropertyType();

            // ReSharper disable once JoinDeclarationAndInitializer
            IQueryable<TEntity> modified;

            modified = this.ModifyQuery<string>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<DateTime>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<DateTime?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<DateTimeOffset>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<DateTimeOffset?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<decimal>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<decimal?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<Guid>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<Guid?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<bool>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<bool?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<int>(query, propType);
            if(modified != null)
                return modified;

            modified = this.ModifyQuery<int?>(query, propType);
            if(modified != null)
                return modified;

            modified = this.ModifyQuery<uint>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<uint?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<long>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<long?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<ulong>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<ulong?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<byte>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<byte?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<sbyte>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<sbyte?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<float>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<float?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<double>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<double?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<short>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<short?>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<ushort>(query, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyQuery<ushort?>(query, propType);
            if (modified != null)
                return modified;

            throw new Exception(string.Format("Type \"{0}\" of {1}.{2} is not supported for dynamic sorting.", propType.FormatCSharp(), typeof(TEntity).FormatCSharp(), this.PropertyName));
        }

        // ReSharper disable PossibleMultipleEnumeration
        IEnumerable<TEntity> IQueryModifier<TEntity>.Augment(IEnumerable<TEntity> collection)
        {
            Type propType = this.GetPropertyType();

            // ReSharper disable once JoinDeclarationAndInitializer
            IEnumerable<TEntity> modified;

            modified = this.ModifyCollection<string>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<DateTime>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<DateTime?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<DateTimeOffset>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<DateTimeOffset?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<decimal>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<decimal?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<Guid>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<Guid?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<bool>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<bool?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<int>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<int?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<uint>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<uint?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<long>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<long?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<ulong>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<ulong?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<byte>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<byte?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<sbyte>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<sbyte?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<float>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<float?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<double>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<double?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<short>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<short?>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<ushort>(collection, propType);
            if (modified != null)
                return modified;

            modified = this.ModifyCollection<ushort?>(collection, propType);
            if (modified != null)
                return modified;

            throw new Exception(string.Format("Type \"{0}\" of {1}.{2} is not supported for dynamic sorting.", propType.FormatCSharp(), typeof(TEntity).FormatCSharp(), this.PropertyName));
        }
        // ReSharper restore PossibleMultipleEnumeration

        [Pure]
        private Type GetPropertyType()
        {
            PropertyInfo pi = typeof(TEntity).GetProperty(this.PropertyName);
            if (pi == null)
                throw new Exception(string.Format("\"{0}\" is not a property of type \"{1}\".", this.PropertyName, typeof(TEntity).FormatCSharp()));

            Type propType = pi.PropertyType;
            return propType;
        }

        private IQueryable<TEntity> ModifyQuery<TKey>(IQueryable<TEntity> query, Type propType) 
        {
            if(propType != typeof(TKey))
                return null;

            Expression<Func<TEntity, TKey>> sortExpression = PredicateBuilder.GetSortingExpression<TEntity, TKey>(this.PropertyName);
            return this.IsAscending ? query.OrderBy(sortExpression) : query.OrderByDescending(sortExpression);
        }

        private IEnumerable<TEntity> ModifyCollection<TKey>(IEnumerable<TEntity> colletion, Type propType)
        {
            if (propType != typeof(TKey))
                return null;

            Func<TEntity, TKey> sortExpression = PredicateBuilder.GetSortingExpression<TEntity, TKey>(this.PropertyName).Compile();
            return this.IsAscending ? colletion.OrderBy(sortExpression) : colletion.OrderByDescending(sortExpression);
        }
    }

    #endregion Stock Query Modifier Classes

    internal static partial class InternalExtensions
    {
        internal static IQueryModifier<TEntity>[] GetModifiers<TEntity>(this QueryModifiers cmod)
        {
            if(cmod == null)
                return null;

            List<IQueryModifier<TEntity>> modifiers = new List<IQueryModifier<TEntity>>();

            if(cmod.Filters != null)
                modifiers.AddRange(cmod.Filters.Where(f => f != null).Select(f => f.GetModifier<TEntity>()));

            if(cmod.Sorting != null)
                modifiers.AddRange(cmod.Sorting.Where(s => s != null).Select(s => s.GetModifier<TEntity>()));

            if(cmod.Paging != null)
                modifiers.Add(cmod.Paging.GetModifier<TEntity>());

            return modifiers.Count == 0 ? null : modifiers.ToArray();
        }
    }
}
