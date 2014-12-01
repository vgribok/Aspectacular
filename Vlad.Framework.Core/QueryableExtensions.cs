#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Aspectacular
{
    public static class QueryableExtensions
    {
        /// <summary>
        ///     More efficient version of Any() when applied to Entity Framework.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Lifted from http://stackoverflow.com/questions/4722541/optimizing-linq-any-call-in-entity-framework
        /// </remarks>
        public static bool Exists<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate)
        {
            return queryable.Where(predicate).Select(x => (int?)1).FirstOrDefault().HasValue;
        }

        /// <summary>
        ///     Better than Any() for T-SQL queries.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Lifted from http://stackoverflow.com/questions/4722541/optimizing-linq-any-call-in-entity-framework
        /// </remarks>
        public static bool Exists<T>(this IQueryable<T> queryable)
        {
            return queryable.Select(x => (int?)1).FirstOrDefault().HasValue;
        }

        /// <summary>
        /// Modifies query to bring a given page of data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size in the number of records</param>
        /// <returns></returns>
        public static IQueryable<TEntity> Page<TEntity>(this IQueryable<TEntity> query, int pageIndex, int pageSize)
        {
            if (query == null)
                return null;

            int skipCount = CalcSkip(pageIndex, pageSize);
            return query.Skip(skipCount).Take(pageSize);
        }

        /// <summary>
        /// Modifies query and trip the wire with ToList() to bring a given page of data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size in the number of records</param>
        /// <returns></returns>
        public static IList<TEntity> PageList<TEntity>(this IQueryable<TEntity> query, int pageIndex, int pageSize)
        {
            return query == null ? null : query.Page(pageIndex, pageSize).ToList();
        }

        /// <summary>
        /// Brings given page of data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection"></param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size in the number of records</param>
        /// <returns></returns>
        public static IEnumerable<TEntity> Page<TEntity>(this IEnumerable<TEntity> collection, int pageIndex, int pageSize)
        {
            if (collection == null)
                return null;

            int skipCount = CalcSkip(pageIndex, pageSize);
            return collection.Skip(skipCount).Take(pageSize);
        }

        /// <summary>
        /// Brings given page of data.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="collection"></param>
        /// <param name="pageIndex">Zero-based page index.</param>
        /// <param name="pageSize">Page size in the number of records</param>
        /// <returns></returns>
        public static IList<TEntity> PageList<TEntity>(this IEnumerable<TEntity> collection, int pageIndex, int pageSize)
        {
            return collection == null ? null : collection.Page(pageIndex, pageSize).ToList();
        }

        private static int CalcSkip(int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
                throw new ArgumentException("pageIndex parameter cannot be negative.");
            if (pageSize < 1)
                throw new ArgumentException("pageSize parameter must be greater than 0.");

            return pageIndex * pageSize;
        }
    }
}