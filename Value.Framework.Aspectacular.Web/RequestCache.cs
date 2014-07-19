#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Web;

namespace Aspectacular
{
    /// <summary>
    ///     Caches data for the duration of the HTTP request processing.
    ///     Should be used together with [InvariantReturn] attribute
    ///     applied to methods and classes whose results can be cached.
    /// </summary>
    /// <remarks>
    ///     No explicit cleanup/invalidation is required as cached data
    ///     is dropped at the end of the request processing.
    ///     This caching mechanism could be used to eliminate same queries
    ///     to the database done within the scope of a single page.
    ///     Please note that method unique signature involves a very slow method parameter evaluation.
    ///     and therefore only methods with high likelihood of repeated calls should be cached.
    /// </remarks>
    public class RequestCache : ICacheProvider
    {
        /// <summary>
        ///     Please use static Get() factory method instead of the constructor.
        /// </summary>
        protected RequestCache()
        {
        }

        #region ICacheProvider implementation

        public void Set(string key, object val)
        {
            lock(HttpContext.Current.Items)
            {
                HttpContext.Current.Items["RequestCache_" + key] = val;
            }
        }

        public bool TryGet(string key, out object val)
        {
            val = null;

            key = "RequestCache_" + key;

            lock(HttpContext.Current.Items)
            {
                if(!HttpContext.Current.Items.Contains(key))
                    return false;

                val = HttpContext.Current.Items[key];
            }

            return true;
        }

        #endregion ICacheProvider implementation

        /// <summary>
        ///     No reason to instantiate destroy it - it has no state and works on the current request.
        /// </summary>
        protected static readonly RequestCache requestCacher = new RequestCache();

        /// <summary>
        ///     Factory returning instance of the http request-level cache provider for caching aspect.
        /// </summary>
        /// <returns></returns>
        public static RequestCache Get()
        {
            return requestCacher;
        }
    }
}