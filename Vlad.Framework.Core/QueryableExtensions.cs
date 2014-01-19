using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    public enum SortOrder
    {
        Ascending = 1, Descending
    }

    public static class QueryableExtensions
    {
        /// <summary>
        /// More efficient version of Any() when applied to Entity Framework. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <remarks>
        /// Lifted from http://stackoverflow.com/questions/4722541/optimizing-linq-any-call-in-entity-framework
        /// </remarks>
        public static bool Exists<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
        {
            return queryable.Where(predicate).Select(x => (int?)1).FirstOrDefault().HasValue;
        }

        /// <summary>
        /// Better than Any() for T-SQL queries.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        /// <remarks>
        /// Lifted from http://stackoverflow.com/questions/4722541/optimizing-linq-any-call-in-entity-framework
        /// </remarks>
        public static bool Exists<T>(this IQueryable<T> queryable)
        {
            return queryable.Select(x => (int?)1).FirstOrDefault().HasValue;
        }

        /// <summary>
        /// Returns sequence of objects ordered by the value of the given property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="entityPropertyName">Property by which collection will be ordered.</param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderByProperty<T>(this IEnumerable<T> collection, string entityPropertyName, SortOrder order = SortOrder.Ascending)
        {
            if (collection == null)
                return null;

            Type entityType = typeof(T);
            PropertyInfo pi = entityType.GetProperty(entityPropertyName);
            if (pi == null)
                throw new ArgumentException("Property \"{0}\" was not found in type \"{1}\".".SmartFormat(entityPropertyName, entityType.FormatCSharp()));

            IEnumerable<T> ordered;

            if(order == SortOrder.Ascending)
                ordered = collection.OrderBy(r => pi.GetValue(r, null));
            else
                ordered = collection.OrderByDescending(r => pi.GetValue(r, null));

            return ordered;
        }
        
        /// <summary>
        /// Returns sequence of objects ordered by the value of the given field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="entityFieldName">Class field by which collection will be ordered.</param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderByField<T>(this IEnumerable<T> collection, string entityFieldName, SortOrder order = SortOrder.Ascending)
        {
            if (collection == null)
                return null;

            Type entityType = typeof(T);
            FieldInfo fi = entityType.GetField(entityFieldName);
            if (fi == null)
                throw new ArgumentException("Field \"{0}\" was not found in type \"{1}\".".SmartFormat(entityFieldName, entityType.FormatCSharp()));

            IEnumerable<T> ordered;

            if (order == SortOrder.Ascending)
                ordered = collection.OrderBy(r => fi.GetValue(r));
            else
                ordered = collection.OrderByDescending(r => fi.GetValue(r));

            return ordered;
        }
    }
}
