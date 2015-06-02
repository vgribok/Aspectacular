#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aspectacular
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty(this ICollection collection)
        {
            return collection == null || collection.Count == 0;
        }

        public static bool IsNullOrEmpty(this IEnumerable collection)
        {
            if(collection == null)
                return true;

            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once UnusedVariable
            foreach(object first in collection)
                return false;

            return true;
        }

        /// <summary>
        ///     Rearranges elements of the collection in the opposite order.
        ///     [A, B, C, D] becomes [D, C, B, A].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IEnumerable<T> ReverseOrder<T>(this IEnumerable<T> collection)
        {
            if(collection == null)
                return null;

            return collection.Reverse();
        }

        /// <summary>
        ///     Lambda-style foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            if(collection == null)
                return;

            foreach(T elem in collection)
                func(elem);
        }

        /// <summary>
        ///     Lambda-style foreach loop.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForEach(this IEnumerable collection, Action<object> func)
        {
            if(collection == null)
                return;

            foreach(object elem in collection)
                func(elem);
        }

        /// <summary>
        /// Lambda-style foreach loop starting new task to handle each element in the collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="asyncFunc">Thread-safe handler of a collection element.</param>
        /// <returns></returns>
        public static IEnumerable<Task> ForEachAsync(this IEnumerable collection, Action<object> asyncFunc)
        {
            if(collection == null)
                return null;

            return collection.ForEachAsync(asyncFunc, CancellationToken.None);
        }

        /// <summary>
        /// Lambda-style foreach loop starting new task to handle each element in the collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="asyncFunc">Thread-safe handler of a collection element.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of Task objects</returns>
        public static IEnumerable<Task> ForEachAsync(this IEnumerable collection, Action<object> asyncFunc, CancellationToken cancellationToken)
        {
            foreach(object elem in collection)
            {
                object param = elem;
                yield return Task.Factory.StartNew(() => asyncFunc(param), cancellationToken);
            }
        }

        /// <summary>
        /// Lambda-style foreach loop starting new task to handle each element in the collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="asyncFunc">Thread-safe handler of a collection element.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Task> ForEachAsync<T>(this IEnumerable<T> collection, Action<T> asyncFunc)
        {
            return collection.ForEachAsync(asyncFunc, CancellationToken.None);
        }

        /// <summary>
        /// Lambda-style foreach loop starting new task to handle each element in the collection.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="asyncFunc">Thread-safe handler of a collection element.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>List of Task objects</returns>
        public static IEnumerable<Task> ForEachAsync<T>(this IEnumerable<T> collection, Action<T> asyncFunc, CancellationToken cancellationToken)
        {
            if (collection == null)
                return null;

            return collection.Select(elem => Task.Factory.StartNew(() => asyncFunc(elem), cancellationToken));
        }


        /// <summary>
        ///     Lambda-style for loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void For<T>(this IList<T> collection, Action<IList<T>, int> func)
        {
            if(collection == null)
                return;

            for(int i = 0; i < collection.Count; i++)
                func(collection, i);
        }

        /// <summary>
        ///     Lambda-style for loop
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForLoop(this IList collection, Action<IList, int> func)
        {
            if(collection == null)
                return;

            for(int i = 0; i < collection.Count; i++)
                func(collection, i);
        }

        /// <summary>
        ///     Calls [handler] given number of times and passes zero-based iteration index to it.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="handler"></param>
        public static void Repeat(this int count, Action<int> handler)
        {
            for(int i = 0; i < count; i++)
                handler(i);
        }

        /// <summary>
        ///     Calls [handler] given number of times and passes zero-based iteration index to it.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="handler"></param>
        public static void Repeat(this long count, Action<long> handler)
        {
            for(long i = 0; i < count; i++)
                handler(i);
        }

        public static IEnumerable ToEnumerable(this IQueryable query)
        {
            return Enumerable.Cast<object>(query);
        }

        /// <summary>
        ///     More convenient form of Union().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> More<T>(this IEnumerable<T> addToCollection, params T[] items)
        {
            return SmartUnion(addToCollection, items);
        }

        /// <summary>
        ///     A better version of the Union() method that won't trip up on null collections.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="leftCollection"></param>
        /// <param name="rightCollection"></param>
        /// <returns></returns>
        public static IEnumerable<T> SmartUnion<T>(this IEnumerable<T> leftCollection, IEnumerable<T> rightCollection)
        {
            if(leftCollection == null)
                return rightCollection;

            if(rightCollection == null)
                return leftCollection;

            return leftCollection.Union(rightCollection);
        }


        /// <summary>
        ///     Returns sequence of objects ordered by the value of the given property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="entityPropertyName">Property by which collection will be ordered.</param>
        /// <param name="orderAscending"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderByProperty<T>(this IEnumerable<T> collection, string entityPropertyName, bool orderAscending = true)
        {
            IEnumerable<T> ordered = collection.OrderByMember(entityPropertyName, orderAscending,
                (propName, entity) => entity.GetType().GetPropertyValue(propName, entity)
                );
            return ordered;
        }

        /// <summary>
        ///     Returns sequence of objects ordered by the value of the given field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="entityFieldName">Class field by which collection will be ordered.</param>
        /// <param name="orderAscending"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderByField<T>(this IEnumerable<T> collection, string entityFieldName, bool orderAscending = true)
        {
            IEnumerable<T> ordered = collection.OrderByMember(entityFieldName, orderAscending,
                (fldName, entity) => entity.GetType().GetMemberFieldValue(fldName, entity)
                );
            return ordered;
        }

        private static IEnumerable<TEntity> OrderByMember<TEntity>(this IEnumerable<TEntity> collection, string memberName, bool orderAscending, Func<string, object, object> memberValueReader)
        {
            if(collection == null || string.IsNullOrEmpty(memberName))
                return collection;

            IEnumerable<TEntity> ordered;

            if(orderAscending)
                ordered = collection.OrderBy(r => memberValueReader(memberName, r));
            else
                ordered = collection.OrderByDescending(r => memberValueReader(memberName, r));

            return ordered;
        }

        /// <summary>
        ///     Returns null if value was not found in the dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal">Value type</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TVal? GetValueSafe<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key)
            where TVal : struct
        {
            TVal val;
            if(!dictionary.TryGetValue(key, out val))
                return null;

            return val;
        }

        /// <summary>
        /// Returns defaultValue if key is not found in the dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TVal GetValueSafe<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key, TVal defaultValue)
            where TVal : struct
        {
            TVal val;
            if (!dictionary.TryGetValue(key, out val))
                return defaultValue;

            return val;
        }

        /// <summary>
        ///     Returns defaultValue if value was not found in the dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal">Reference type</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue">Null by default</param>
        /// <returns></returns>
        public static TVal GetObjectValueSafe<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key, TVal defaultValue = null)
            where TVal : class
        {
            TVal val;
            if(!dictionary.TryGetValue(key, out val))
                return defaultValue;

            return val;
        }

        /// <summary>
        ///     Compares two sets, Current and New one. New set determines the end state.
        ///     Result of this method is two sets: items to be deleted from the old set
        ///     and items to be added to the current set in order to make up new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentSet"></param>
        /// <param name="newSet"></param>
        /// <param name="equalityChecker"></param>
        /// <returns></returns>
        public static SetComparisonResult<T> CompareSets<T>(this IEnumerable<T> currentSet, IEnumerable<T> newSet, Func<T, T, bool> equalityChecker)
        {
            var retVal = new SetComparisonResult<T>();

            currentSet = currentSet ?? new T[0];

            if(newSet == null)
            {
                retVal.ToBeAdded = new T[0];
                retVal.ToBeDeleted = currentSet;
            } else
            {
                IEqualityComparer<T> icomparer = ToIEqualityComparer(equalityChecker);
                IList<T> newSetList = newSet.ToList();
                retVal.ToBeAdded = newSetList.Where(newItem => !currentSet.Contains(newItem, icomparer));
                retVal.ToBeDeleted = currentSet.Where(existing => !newSetList.Contains(existing, icomparer));
            }

            return retVal;
        }

        /// <summary>
        ///     Compares two sets, Current and New one. New set determines the end state.
        ///     Result of this method is two sets: items to be deleted from the old set
        ///     and items to be added to the current set in order to make up new one.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentSet"></param>
        /// <param name="newSet"></param>
        /// <returns></returns>
        public static SetComparisonResult<T> CompareSets<T>(this IEnumerable<T> currentSet, IEnumerable<T> newSet) where T : IEquatable<T>
        {
            return CompareSets(currentSet, newSet, AreEqualEquitable);
        }

        /// <summary>
        ///     Returns true if either both sets contain same elements, or both are empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <param name="equalityChecker"></param>
        /// <returns></returns>
        public static bool HaveSameElements<T>(this IEnumerable<T> set1, IEnumerable<T> set2, Func<T, T, bool> equalityChecker)
        {
            if(equalityChecker == null)
                throw new ArgumentNullException("equalityChecker");

            if(set1 == null && set2 == null)
                return true;

            set1 = set1 ?? new T[0]; // I dare anyone to say I don't appreciate JavaScript :-).
            set2 = set2 ?? new T[0];

            IEnumerator<T> enumerator1 = set1.GetEnumerator();
            IEnumerator<T> enumerator2 = set2.GetEnumerator();

            bool next1, next2;
            do
            {
                next1 = enumerator1.MoveNext();
                next2 = enumerator2.MoveNext();

                if(next1 != next2)
                    return false;

                if(!next1)
                    return true;

                if(!equalityChecker(enumerator1.Current, enumerator2.Current))
                    return false;
            } while(true);
        }

        /// <summary>
        ///     Returns true if either both sets contain same elements, or both are empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <returns></returns>
        public static bool HaveSameElements<T>(this IEnumerable<T> set1, IEnumerable<T> set2) where T : IEquatable<T>
        {
            return HaveSameElements(set1, set2, AreEqualEquitable);
        }

        public static bool AreEqualEquitable<T>(this T x, T y) where T : IEquatable<T>
        {
            if((object)x == null || (object)y == null)
                return (object)x == null && (object)y == null;

            // neither is null
            return x.Equals(y);
        }

        /// <summary>
        ///     It stinks to implement IEqualityComparer for each entity.
        ///     This allows for supplying comparer closure instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="equalityChecker"></param>
        /// <returns></returns>
        public static IEqualityComparer<T> ToIEqualityComparer<T>(Func<T, T, bool> equalityChecker)
        {
            return new SimpleComparer<T>(equalityChecker);
        }

        /// <summary>
        ///     Returns IEqualityComparer implementation for IEquatable types, like int, string, DateTime, etc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEqualityComparer<T> GetIEqualityComparer<T>() where T : IEquatable<T>
        {
            return new SimpleComparer<T>(AreEqualEquitable);
        }
    }

    /// <summary>
    ///     Converter of Func[T, T, bool] to IEqualityComparer[T].
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleComparer<T> : IEqualityComparer<T>
    {
        protected readonly Func<T, T, bool> equalityChecker;

        public SimpleComparer(Func<T, T, bool> equalityChecker)
        {
            if(equalityChecker == null)
                throw new ArgumentNullException("equalityChecker");

            this.equalityChecker = equalityChecker;
        }

        public bool Equals(T x, T y)
        {
            return this.equalityChecker(x, y);
        }

        public int GetHashCode(T obj)
        {
            return !typeof(T).IsValueType && (object)obj == null ? 0 : obj.GetHashCode();
        }
    }

    /// <summary>
    ///     Result of comparing two sets: New and Current.
    ///     After comparison, items present in New but missing in Current,
    ///     will be added to the ToBeAdded collection,
    ///     and items present in Current but missing in New, will be added to ToBeDeleted collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SetComparisonResult<T>
    {
        public IEnumerable<T> ToBeAdded { get; set; }
        public IEnumerable<T> ToBeDeleted { get; set; }
    }

    /// <summary>
    ///     A convenience class representing a pair of arbitrary values.
    ///     Works with KeyValuePair via implicit conversion.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    [Obsolete("Use Tuple<> from .NET Framework.")]
    public class Pair<T1, T2>
    {
        public readonly T1 First;
        public readonly T2 Second;

        public Pair(T1 first, T2 second)
        {
            this.First = first;
            this.Second = second;
        }

        public Pair(KeyValuePair<T1, T2> pair)
            : this(pair.Key, pair.Value)
        {
        }

        public static implicit operator Pair<T1, T2>(KeyValuePair<T1, T2> kvPair)
        {
            return new Pair<T1, T2>(kvPair);
        }

        public static implicit operator KeyValuePair<T1, T2>(Pair<T1, T2> pair)
        {
            return new KeyValuePair<T1, T2>(pair.First, pair.Second);
        }
    }
}