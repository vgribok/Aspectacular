﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
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
        /// foreach loop done with a delegate.
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
        /// foreach loop done with a delegate.
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
    }
}
