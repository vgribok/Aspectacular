using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Aspectacular
{
    public static partial class QueryableExtensions
    {
        /// <summary>
        /// Implements in-memory full outer join of two sets.
        /// </summary>
        /// <typeparam name="TLeft">Entity type of the left set of the join</typeparam>
        /// <typeparam name="TRight">Entity type of the right set of the join</typeparam>
        /// <typeparam name="TKey">Type of the join field</typeparam>
        /// <typeparam name="TResult">Entity type of the result set</typeparam>
        /// <param name="left">Left set of the join</param>
        /// <param name="right">Right set of the join</param>
        /// <param name="leftKeySelector">Delegate returning join column from the left set</param>
        /// <param name="rightKeySelector">Delegate returning join column from the right set</param>
        /// <param name="resultSelector">Delegate returning result entity based on two entities - one left and one right.</param>
        /// <returns>Set including full outer join</returns>
        public static IEnumerable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IEnumerable<TLeft> left, IEnumerable<TRight> right,
                            Func<TLeft, TKey> leftKeySelector, Func<TRight, TKey> rightKeySelector,
                            Func<TLeft, TRight, TResult> resultSelector)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            if (right == null)
                throw new ArgumentNullException("right");
            if (leftKeySelector == null)
                throw new ArgumentNullException("leftKeySelector");
            if (rightKeySelector == null)
                throw new ArgumentNullException("rightKeySelector");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");

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


        /// <summary>
        /// Implements full outer join as a query that can be translated to T-SQL and other IQueryable-aware engines,
        /// and executed at the database engine.
        /// </summary>
        /// <typeparam name="TLeft">Entity type of the left set of the join</typeparam>
        /// <typeparam name="TRight">Entity type of the right set of the join</typeparam>
        /// <typeparam name="TKey">Type of the join field</typeparam>
        /// <typeparam name="TResult">Entity type of the result set</typeparam>
        /// <param name="left">Left set of the join</param>
        /// <param name="right">Right set of the join</param>
        /// <param name="leftKeySelector">Expression returning join column from the left set</param>
        /// <param name="rightKeySelector">Expression returning join column from the right set</param>
        /// <param name="resultSelector">Expression returning result entity based on two entities - one left and one right.</param>
        /// <returns>Query implementing full outer join</returns>
        public static IQueryable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IQueryable<TLeft> left, IQueryable<TRight> right,
                                            Expression<Func<TLeft, TKey>> leftKeySelector, Expression<Func<TRight, TKey>> rightKeySelector,
                                            Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            if(left == null)
                throw new ArgumentNullException("left");
            if(right == null)
                throw new ArgumentNullException("right");
            if(leftKeySelector == null)
                throw new ArgumentNullException("leftKeySelector");
            if(rightKeySelector == null)
                throw new ArgumentNullException("rightKeySelector");
            if(resultSelector == null)
                throw new ArgumentNullException("resultSelector");

            IQueryable<TResult> leftJoin = left.GroupJoin(right, leftKeySelector, rightKeySelector,
                                    (l, rs) => new OuterJoinTemp<TLeft, TRight> { Outer = l, InnerSet = rs })
                                    .SelectMany(ojt => ojt.InnerSet.DefaultIfEmpty(), MakeJoinExpressionLeft(resultSelector));

            IQueryable<TResult> rightJoin = right.GroupJoin(left, rightKeySelector, leftKeySelector,
                                    (r, ls) => new OuterJoinTemp<TRight, TLeft> { Outer = r, InnerSet = ls })
                                    .SelectMany(ojt => ojt.InnerSet.DefaultIfEmpty(), MakeJoinExpressionRight(resultSelector));

            var fullOuter = leftJoin.Union(rightJoin);
            return fullOuter;
        }

        #region Private Members

        private class OuterJoinTemp<TOuter, TInner>
        {
            internal TOuter Outer { get; set; }
            internal IEnumerable<TInner> InnerSet { get; set; }
        }

        private static Expression<Func<OuterJoinTemp<TLeft, TRight>, TRight, TResult>>
            MakeJoinExpressionLeft<TLeft, TRight, TResult>(Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            //  User-provided exp:                  // Need to generate exp:
            // (l, r) => new TResult                 (j, r) => new TResult         
            //    {                                     {
            //            //ID1 = l.ID,                   //ID1 = j.Outer.ID,
            //            //ID2 = r.ID                    //ID2 = r.ID
            //    }                                     }

            var ojtParmExp = Expression.Parameter(typeof(OuterJoinTemp<TLeft, TRight>), "_ojtl");
            Expression outerPropertyAccessExpression = Expression.Property(ojtParmExp, "Outer");
            ParameterReplacer reparmer = new ParameterReplacer(resultSelector.Parameters[0], outerPropertyAccessExpression);
            Expression body = reparmer.Visit(resultSelector.Body);

            var retExp = Expression.Lambda<Func<OuterJoinTemp<TLeft, TRight>, TRight, TResult>>(body, ojtParmExp, resultSelector.Parameters[1]);
            return retExp;
        }

        private static Expression<Func<OuterJoinTemp<TRight, TLeft>, TLeft, TResult>>
            MakeJoinExpressionRight<TLeft, TRight, TResult>(Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
            //  User-provided exp:                  // Need to generate exp:
            // (l, r) => new TResult                (j, l) => new TResult 
            //    {                                     { 
            //            //ID1 = l.ID,                         //ID1 = l.ID, 
            //            //ID2 = r.ID                          //ID2 = j.Outer.ID 
            //    }                                     } 

            var ojtParmExp = Expression.Parameter(typeof(OuterJoinTemp<TRight, TLeft>), "_ojtr");
            Expression outerPropertyAccessExpression = Expression.Property(ojtParmExp, "Outer");
            ParameterReplacer reparmer = new ParameterReplacer(resultSelector.Parameters[1], outerPropertyAccessExpression);
            Expression body = reparmer.Visit(resultSelector.Body);

            var retExp = Expression.Lambda<Func<OuterJoinTemp<TRight, TLeft>, TLeft, TResult>>(body, ojtParmExp, resultSelector.Parameters[0]);
            return retExp;
        }


        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly string paramName;
            private readonly Type paramType;
            private readonly Expression replacementExpression;

            internal ParameterReplacer(ParameterExpression parmToReplace, Expression replacementExpression)
                : this(parmToReplace.Name, parmToReplace.Type, replacementExpression)
            {
            }

            private ParameterReplacer(string paramName, Type paramType, Expression replacementExpression)
            {
                this.paramName = paramName;
                this.paramType = paramType;
                this.replacementExpression = replacementExpression;
            }

            /// <summary>
            /// Visits the <see cref="T:System.Linq.Expressions.ParameterExpression"/>.
            /// </summary>
            /// <returns>
            /// The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.
            /// </returns>
            /// <param name="node">The expression to visit.</param>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node.Name == this.paramName && node.Type == this.paramType)
                    return this.replacementExpression;

                return base.VisitParameter(node);
            }
        }

        #endregion Private Members
    }
}
