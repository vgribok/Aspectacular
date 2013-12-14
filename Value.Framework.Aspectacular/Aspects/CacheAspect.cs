using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular.Aspects
{
    public interface ICacheProvider
    {
        void Set(string key, object val);
        bool TryGet(string key, out object val);
    }

    /// <summary>
    /// Note: Building method caching key is very expensive performance-wise.
    /// Use it only to cache data that store in slow storage types, like databases, files, on the network, on the Internet, etc.
    /// </summary>
    /// <typeparam name="TCacher"></typeparam>
    /// <remarks>
    /// Please note that CacheAspect will not attempt to invalidate the cache - it's a responsibility of the ICacheProvider implementation.
    /// </remarks>
    public class CacheAspect<TCacher> : Aspect
        where TCacher : ICacheProvider
    {
        protected TCacher Cache { get; private set; }
        
        public bool ValueFoundInCache { get; private set; }

        public CacheAspect(TCacher cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            this.Cache = cacheProvider;

            this.LogInformation("Cache provider type", cacheProvider.GetType().FullName);
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformation("Method is cacheable", this.Context.CanCacheReturnedResult);

            if (!this.Context.CanCacheReturnedResult)
                return;

            this.GetValueFromCacheIfItsThere();
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if (!this.Context.CanCacheReturnedResult)
                return;

            if (this.ValueFoundInCache)
                return;

            this.SaveValueToCache();
        }

        private void SaveValueToCache()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();
            object val = this.Context.InterceptedMedthodCallFailed ? this.Context.MethodExecutionException : this.Context.ReturnedValue;
            this.Cache.Set(cacheKey, val);
        }

        private void GetValueFromCacheIfItsThere()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();

            object cachedValue;
            this.ValueFoundInCache = this.Cache.TryGet(cacheKey, out cachedValue);

            this.LogInformation("Found in cache", this.ValueFoundInCache);

            if (this.ValueFoundInCache)
            {
                if (cachedValue is Exception)
                    this.Context.MethodExecutionException = cachedValue as Exception;
                else
                    this.Context.ReturnedValue = cachedValue;

                this.Context.CancelInterceptedMethodCall = true;
            }
        }

        protected string BuildMethodCacheKeyVerySlowly()
        {
            return this.Context.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);
        }
    }
}
