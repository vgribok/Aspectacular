#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
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
    }
}