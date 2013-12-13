using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Value.Framework.Core;
//using Value.Framework.Aspectacular.Aspects;

namespace Value.Framework.Aspectacular.Aspects
{
    /// <summary>
    /// Caches data for the duration of the request processing.
    /// Should be used together with 
    /// </summary>
    /// <remarks>
    /// No explicit cleanup/invalidation is required as cached data 
    /// is dropped at the end of the request processing.
    /// This caching mechanism could be used to eliminate same queries
    /// to the database done within the scope of a single page.
    /// Please note that method unique signature involves a very slow method parameter evaluation.
    /// and therefore only methods with high likelihood of repeated calls should be cached.
    /// </remarks>
    public class RequestCache : ICacheProvider
    {
        /// <summary>
        /// Please use static Get() factory method instead of the constructor.
        /// </summary>
        protected RequestCache() { }

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

            lock (HttpContext.Current.Items)
            {
                if (!HttpContext.Current.Items.Contains(key))
                    return false;

                val = HttpContext.Current.Items[key];
            }

            return true;
        }

        #endregion ICacheProvider implementation

        /// <summary>
        /// No reason to instantiate destroy it - it has no state and works on the current request.
        /// </summary>
        protected static readonly RequestCache requestCacher = new RequestCache();

        /// <summary>
        /// Factory returning instance of the http request-level cache provider for caching aspect.
        /// </summary>
        /// <returns></returns>
        public static RequestCache Get()
        {
            return requestCacher;
        }
    }

    /// <summary>
    /// An aspect caching invariant intercepted function's returned values in the Request.Items[] collection.
    /// </summary>
    public class RequestCachAspect : CacheAspect<RequestCache>
    {
        public RequestCachAspect() : base(RequestCache.Get())
        {
        }
    }
}
