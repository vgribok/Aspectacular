using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

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
            if (collection == null)
                return true;

            foreach (object first in collection)
                return false;

            return true;
        }

        /// <summary>
        /// Rearranges elements of the collection in the opposite order.
        /// [A, B, C, D] becomes [D, C, B, A].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static List<T> ReverseOrder<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                return null;

            List<T> backwards = new List<T>();
            foreach (T elem in collection)
                backwards.Insert(0, elem);

            return backwards;
        }

        /// <summary>
        /// Lambda-style foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            if (collection == null)
                return;

            foreach (T elem in collection)
                func(elem);
        }

        /// <summary>
        /// Lambda-style foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForEach(this IEnumerable collection, Action<object> func)
        {
            if (collection == null)
                return;

            foreach (object elem in collection)
                func(elem);
        }

        /// <summary>
        /// Lambda-style for loop
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void For<T>(this IList<T> collection, Action<IList<T>, int> func)
        {
            if (collection == null)
                return;

            for (int i = 0; i < collection.Count; i++)
                func(collection, i);
        }

        /// <summary>
        /// Lambda-style for loop
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        public static void ForLoop(this IList collection, Action<IList, int> func)
        {
            if (collection == null)
                return;

            for (int i = 0; i < collection.Count; i++)
                func(collection, i);
        }

        public static IEnumerable ToEnumerable(this IQueryable query)
        {
            foreach (object elem in query)
                yield return elem;
        }

        /// <summary>
        /// More convenient form of Union().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addToCollection"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public static IEnumerable<T> More<T>(this IEnumerable<T> addToCollection, params T[] items)
        {
            if (addToCollection == null)
                return items;

            if(items == null)
                return addToCollection;

            return addToCollection.Union(items);
        }


        /// <summary>
        /// Returns sequence of objects ordered by the value of the given property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="entityPropertyName">Property by which collection will be ordered.</param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static IEnumerable<T> OrderByProperty<T>(this IEnumerable<T> collection, string entityPropertyName, bool orderAscending = true)
        {
            IEnumerable<T> ordered = collection.OrderByMember(entityPropertyName, orderAscending,
                                                (propName, entity) => entity.GetType().GetPropertyValue(propName, entity)
                                            );
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
        public static IEnumerable<T> OrderByField<T>(this IEnumerable<T> collection, string entityFieldName, bool orderAscending = true)
        {
            IEnumerable<T> ordered = collection.OrderByMember(entityFieldName, orderAscending, 
                                                (fldName, entity) => entity.GetType().GetMemberFieldValue(fldName, entity)
                                            );
            return ordered;
        }

        private static IEnumerable<TEntity> OrderByMember<TEntity>(this IEnumerable<TEntity> collection, string memberName, bool orderAscending, Func<string, object, object> memberValueReader)
        {
            if (collection == null || string.IsNullOrEmpty(memberName))
                return collection;

            IEnumerable<TEntity> ordered;

            if (orderAscending)
                ordered = collection.OrderBy(r => memberValueReader(memberName, r));
            else
                ordered = collection.OrderByDescending(r => memberValueReader(memberName, r));

            return ordered;
        }

        /// <summary>
        /// Returns null if value was not found in the dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal">Value type</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TVal? GetValueSafe<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, TKey key)
            where TVal : struct
        {
            TVal val;
            if (!dictionary.TryGetValue(key, out val))
                return null;

            return val;
        }

        /// <summary>
        /// Returns null if value was not found in the dictionary.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TVal">Reference type</typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static TVal GetObjectValueSafe<TKey, TVal>(this Dictionary<TKey, TVal> dictionary, TKey key)
            where TVal : class
        {
            TVal val;
            if (!dictionary.TryGetValue(key, out val))
                return null;

            return val;
        }
    }

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
