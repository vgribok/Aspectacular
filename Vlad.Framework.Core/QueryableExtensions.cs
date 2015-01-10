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
        /// Distinct by a given field. Could be slow as it uses GroupBy.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="query"></param>
        /// <param name="keySelector">Expression returning field used to determine distinct</param>
        /// <returns></returns>
        public static IQueryable<TEntity> DistinctSlow<TEntity, TKey>(this IQueryable<TEntity> query, Expression<Func<TEntity, TKey>> keySelector)
        {
            return query.GroupBy(keySelector).Select(r => r.FirstOrDefault());
        }


        #region Full Outer Join Methods

        internal class OuterJoinTemp<TOuter, TInner>
        {
            internal TOuter Outer { get; set; }
            internal IEnumerable<TInner> InnerSet { get; set; }
        }


        public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right,
                            Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector,
                            Func<TLeft, TRight, TResult> resultSelector)
        {
            // ReSharper disable PossibleMultipleEnumeration
            IEnumerable<TResult> leftJoin = left.GroupJoin(right, leftKeySelector, rightKeySelector,
                                    (l, rs) => new OuterJoinTemp<TLeft, TRight> { Outer = l, InnerSet = rs })
                                    .SelectMany(x => x.InnerSet.DefaultIfEmpty(), (s, l) => resultSelector(s.Outer, l));

            IEnumerable<TResult> rightJoin = right.GroupJoin(left, rightKeySelector, leftKeySelector,
                                    (r, ls) => new OuterJoinTemp<TRight, TLeft> { Outer = r, InnerSet = ls })
                                    .SelectMany(x => x.InnerSet.DefaultIfEmpty(), (s, l) => resultSelector(l, s.Outer));
            // ReSharper restore PossibleMultipleEnumeration

            var fullOuter = leftJoin.Union(rightJoin);
            return fullOuter;
        }


        public static IQueryable<TResult> FullOuterJoin<TLeft,TRight,TKey,TResult>(this IQueryable<TLeft> left, 
            IQueryable<TRight> right, 
            Expression<Func<TLeft,TKey>> leftKeySelector, 
            Expression<Func<TRight,TKey>> rightKeySelector, 
            Expression<Func<TLeft,TRight,TResult>> resultSelector) 
            where TResult : new()
        {
            Expression<Func<OuterJoinTemp<TLeft, TRight>, TRight, Tuple<TLeft, TRight>>> leftOuterExp = (j, r) => new Tuple<TLeft, TRight>(j.Outer, r);
            Expression<Func<OuterJoinTemp<TRight, TLeft>, TLeft, Tuple<TLeft, TRight>>> rightOuterExp = (j, r) => new Tuple<TLeft, TRight>(r, j.Outer);

            IQueryable<TResult> leftJoin = left.GroupJoin(right, leftKeySelector, rightKeySelector, 
                                    (l,rs) => new OuterJoinTemp<TLeft, TRight> { Outer = l, InnerSet = rs})
                                    .SelectMany(ojt => ojt.InnerSet.DefaultIfEmpty(), MakeJoinExpressionLeft(resultSelector));
            
            IQueryable<TResult> rightJoin = right.GroupJoin(left, rightKeySelector, leftKeySelector, 
                                    (r, ls) => new OuterJoinTemp<TRight, TLeft> { Outer = r, InnerSet = ls })
                                    .SelectMany(ojt => ojt.InnerSet.DefaultIfEmpty(), MakeJoinExpressionRight(resultSelector));

            var fullOuter = leftJoin.Union(rightJoin);
            return fullOuter;
        }

        private static Expression<Func<OuterJoinTemp<TLeft, TRight>, TRight, TResult>>
            MakeJoinExpressionLeft<TLeft, TRight, TResult>(Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            //(j, r) => new TResult
            //    {
            //            //ID1 = j.Outer.ID,
            //            //ID2 = r.ID
            //    }
            throw new NotImplementedException();
        }

        private static Expression<Func<OuterJoinTemp<TRight, TLeft>, TLeft, TResult>>
            MakeJoinExpressionRight<TLeft, TRight, TResult>(Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            //(j, r) => new TResult
            //    {
            //            //ID1 = j.Outer.ID,
            //            //ID2 = r.ID
            //    }
            throw new NotImplementedException();
        }

        #endregion Full Outer Join Methods
    }
}