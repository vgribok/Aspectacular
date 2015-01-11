using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    public static partial class QueryableExtensions
    {
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


        /// <summary>
        /// Implements full outer join as a query that can be translated to T-SQL and other IQueryable-aware engines,
        /// and executed at the database engine.
        /// </summary>
        /// <typeparam name="TLeft"></typeparam>
        /// <typeparam name="TRight"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="leftKeySelector"></param>
        /// <param name="rightKeySelector"></param>
        /// <param name="resultSelector"></param>
        /// <returns></returns>
        public static IQueryable<TResult> FullOuterJoin<TLeft, TRight, TKey, TResult>(this IQueryable<TLeft> left, IQueryable<TRight> right,
                                            Expression<Func<TLeft, TKey>> leftKeySelector, Expression<Func<TRight, TKey>> rightKeySelector,
                                            Expression<Func<TLeft, TRight, TResult>> resultSelector)
        {
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
