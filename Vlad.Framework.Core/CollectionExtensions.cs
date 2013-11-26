using System;
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
    }
}
