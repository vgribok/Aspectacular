#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Aspectacular
{
    // ReSharper disable CSharpWarnings::CS1591
    /// <summary>
    /// Operators used for building dynamic query filtering expressions
    /// </summary>
    public enum DynamicFilterOperator
    {
        Equal, NotEqual,
        GreaterThan, GreaterThanOrEqual,
        LessThan, LessThanOrEqual,
        StringStartsWith, StringContains
    }
    // ReSharper restore CSharpWarnings::CS1591

    /// <summary>
    ///     Enables the efficient, dynamic composition of query predicates.
    /// </summary>
    /// <remarks>
    ///     Lifted from http://petemontgomery.wordpress.com/2011/02/10/a-universal-predicatebuilder/
    ///     Usage of Predicate builder is explained here: http://www.albahari.com/nutshell/predicatebuilder.aspx
    /// </remarks>
    public static class PredicateBuilder
    {
        #region Predicate combiner

        /// <summary>
        ///     Creates a predicate that evaluates to true.
        /// </summary>
        public static Expression<Func<T, bool>> True<T>()
        {
            return param => true;
        }

        /// <summary>
        ///     Creates a predicate that evaluates to false.
        /// </summary>
        public static Expression<Func<T, bool>> False<T>()
        {
            return param => false;
        }

        /// <summary>
        ///     Creates a predicate expression from the specified lambda expression.
        /// </summary>
        public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate)
        {
            return predicate;
        }

        /// <summary>
        ///     Combines the first predicate with the second using the logical "and".
        /// </summary>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        ///     Combines the first predicate with the second using the logical "or".
        /// </summary>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        /// <summary>
        ///     Negates the predicate.
        /// </summary>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
        {
            UnaryExpression negated = Expression.Not(expression.Body);
            return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
        }

        /// <summary>
        ///     Combines the first expression with the second using the specified merge function.
        /// </summary>
        private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // zip parameters (map from parameters of second to parameters of first)
            Dictionary<ParameterExpression, ParameterExpression> map = first.Parameters
                .Select((f, i) => new {f, s = second.Parameters[i]})
                .ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with the parameters in the first
            Expression secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // create a merged lambda expression with parameters from the first expression
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        private class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> map;

            private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if(map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }

        #endregion Predicate combiner

        #region Dynamic predicate builder

        private static readonly MethodInfo stringStartsWithMethod = typeof(string).GetMethod("StartsWith", new []{ typeof(string) });
        private static readonly MethodInfo stringContainsMethod = typeof(string).GetMethod("Contains");

        // ReSharper disable PossiblyMistakenUseOfParamsMethod

        /// <summary>
        /// Creates predicate expression that can be used in IQueryable[TEntity].Where(entity => entity.PropertyName FilterOperation FilterValue).
        /// Got most from
        /// http://stackoverflow.com/questions/8315819/expression-lambda-and-query-generation-at-runtime-simplest-where-example
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="propertyName">Entity property name</param>
        /// <param name="dynamicFilterOperator">Filter operator, like Equal, StartsWith, Contains, LessThan, etc.</param>
        /// <param name="filterValue">Filter value</param>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>> GetFilterPredicate<TEntity>(string propertyName, DynamicFilterOperator dynamicFilterOperator, object filterValue)
        {
            var entityExp = Expression.Parameter(typeof(TEntity), "entity");
            var propertyExpression = Expression.Property(entityExp, propertyName);

            Expression rvalue = Expression.Constant(filterValue);
            Expression rvalueString = Expression.Constant(filterValue as string);
            Expression boolExpression = null;

            switch (dynamicFilterOperator)
            {
                case DynamicFilterOperator.Equal:
                    boolExpression = Expression.Equal(propertyExpression, rvalue);
                    break;
                case DynamicFilterOperator.NotEqual:
                    boolExpression = Expression.NotEqual(propertyExpression, rvalue);
                    break;
                case DynamicFilterOperator.GreaterThan:
                    boolExpression = Expression.GreaterThan(propertyExpression, rvalue);
                    break;
                case DynamicFilterOperator.GreaterThanOrEqual:
                    boolExpression = Expression.GreaterThanOrEqual(propertyExpression, rvalue);
                    break;
                case DynamicFilterOperator.LessThan:
                    boolExpression = Expression.LessThan(propertyExpression, rvalue);
                    break;
                case DynamicFilterOperator.LessThanOrEqual:
                    boolExpression = Expression.LessThanOrEqual(propertyExpression, rvalue);
                    break;

                case DynamicFilterOperator.StringStartsWith:
                    boolExpression = Expression.Call(propertyExpression, stringStartsWithMethod, rvalueString);
                    break;
                case DynamicFilterOperator.StringContains:
                    boolExpression = Expression.Call(propertyExpression, stringContainsMethod, rvalueString);
                    break;
            }

            if (boolExpression == null)
                throw new InvalidOperationException();

            Expression<Func<TEntity, bool>> exp = Expression.Lambda<Func<TEntity, bool>>(boolExpression, entityExp);

            return exp;
        }

        /// <summary>
        /// Creates OrderBy() or OrderByDescending() expressions for a given TEntity.propertyName combination.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static Expression<Func<TEntity, TKey>> GetSortingExpression<TEntity, TKey>(string propertyName)
        {
            var entityExp = Expression.Parameter(typeof(TEntity), "entity");
            var propertyExpression = Expression.Property(entityExp, propertyName);

            Expression<Func<TEntity, TKey>> exp = Expression.Lambda<Func<TEntity, TKey>>(propertyExpression, entityExp);

            return exp;
        }

        #endregion Dynamic predicate builder
    }
}