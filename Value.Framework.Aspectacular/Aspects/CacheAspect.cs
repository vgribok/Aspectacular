using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Implementing this minimal-dependency interface is
    /// all that's needed to enable caching via CacheAspect.
    /// </summary>
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
        where TCacher : class, ICacheProvider
    {
        protected TCacher Cache { get; private set; }
        
        public bool ValueFoundInCache { get; private set; }

        [Obsolete("Use CacheFactory.CreateCacheAspect() instead.")]
        protected internal CacheAspect(TCacher cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            this.Cache = cacheProvider;
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationWithKey("Cache provider type", this.Cache.GetType().FormatCSharp());
            this.LogInformationData("Method is cacheable", this.Proxy.CanCacheReturnedResult);

            if (!this.Proxy.CanCacheReturnedResult)
                return;

            this.LogParametersWithValues();

            this.GetValueFromCacheIfItsThere();
        }

        private void LogParametersWithValues()
        {
            foreach (var paramInfo in this.Proxy.InterceptedCallMetaData.Params)
            {
                this.LogInformationWithKey("Parameter \"{0}\"".SmartFormat(paramInfo.Name),
                        "Type: [{0}], Value: [{1}]", paramInfo.Type.FormatCSharp(), paramInfo.FormatSlowEvaluatingValue(trueUI_falseInternal: false));
            }
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if (!this.Proxy.CanCacheReturnedResult)
                return;

            if (this.ValueFoundInCache)
                return;

            this.SaveValueToCache();
        }

        private void SaveValueToCache()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();
            object val = this.Proxy.InterceptedMedthodCallFailed ? this.Proxy.MethodExecutionException : this.Proxy.ReturnedValue;
            this.Cache.Set(cacheKey, val);
        }

        private void GetValueFromCacheIfItsThere()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();

            object cachedValue;
            this.ValueFoundInCache = this.Cache.TryGet(cacheKey, out cachedValue);

            this.LogInformationData("Found in cache", this.ValueFoundInCache);

            if (this.ValueFoundInCache)
            {
                if (cachedValue is Exception)
                    this.Proxy.MethodExecutionException = cachedValue as Exception;
                else
                    this.Proxy.ReturnedValue = cachedValue;

                this.Proxy.CancelInterceptedMethodCall = true;
            }
        }

        protected string BuildMethodCacheKeyVerySlowly()
        {
            return this.Proxy.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);
        }
    }

    /// <summary>
    /// A class fronting .NET Framework's mediocre ObjectCache
    /// with Aspectacular-friendly ICacheProvider implementation.
    /// </summary>
    public class ObjectCacheFacade : ICacheProvider
    {
        protected readonly ObjectCache objectCache;
        protected readonly CacheItemPolicy cacheTemplatePolicy;
        protected readonly string regionName;

        /// <param name="cache"></param>
        /// <param name="cachePolicyTemplate">Template from which all items will get their CacheItemPolicy cloned.</param>
        /// <param name="regionName"></param>
        protected internal ObjectCacheFacade(ObjectCache cache, CacheItemPolicy cachePolicyTemplate, string regionName = null)
        {
            if(cache == null)
                throw new ArgumentNullException("cache");

            if(cacheTemplatePolicy == null)
                throw new ArgumentNullException("cacheTemplatePolicy");

            this.objectCache = cache;
            this.cacheTemplatePolicy = cachePolicyTemplate;
            this.regionName = regionName;
        }

        protected CacheItemPolicy ClonePolicy()
        {
            var clonePolicy = new CacheItemPolicy
            {
                 AbsoluteExpiration = this.cacheTemplatePolicy.AbsoluteExpiration,
                 Priority = this.cacheTemplatePolicy.Priority,
                 RemovedCallback = this.cacheTemplatePolicy.RemovedCallback,
                 SlidingExpiration = this.cacheTemplatePolicy.SlidingExpiration,
                 UpdateCallback = this.cacheTemplatePolicy.UpdateCallback,
            };

            this.cacheTemplatePolicy.ChangeMonitors.ForEach(clonePolicy.ChangeMonitors.Add);

            return clonePolicy;
        }

        public void Set(string key, object val)
        {
            this.objectCache.Add(key, val, this.ClonePolicy(), this.regionName);
        }

        public bool TryGet(string key, out object val)
        {
            // It really stinks that MS folks who designed ObjectCache didn't think of "bool TryGetValue(key, out val)" pattern
            // so this thing could be done in one search instead of two. Terrible!

            if (!this.objectCache.Contains(key, this.regionName))
            {
                val = null;
                return false;
            }

            val = this.objectCache.Get(key, this.regionName);
            return true;
        }
    }

    public static class CacheFactory
    {
        /// <summary>
        /// Instantiates new CacheAspect for a given cache provider.
        /// </summary>
        /// <param name="cacheProvider"></param>
        /// <returns></returns>
        public static CacheAspect<ICacheProvider> CreateCacheAspect(this ICacheProvider cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

#pragma warning disable 618
            // ReSharper disable once CSharpWarnings::CS0618
            var cacheAspect = new CacheAspect<ICacheProvider>(cacheProvider);
#pragma warning restore 618
            return cacheAspect;
        }

        /// <summary>
        /// Augments .NET Framework ObjectCache and its descendants into 
        /// Aspectacular-friendly cache provider compatible with the CacheAspect.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="cachePolicyTemplate">.NET Framework object driving cached item expiration.</param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public static ObjectCacheFacade CreateCacheProvider(this ObjectCache cache, CacheItemPolicy cachePolicyTemplate, string regionName = null)
        {
            var ocp = new ObjectCacheFacade(cache, cachePolicyTemplate, regionName);
            return ocp;
        }

        /// <summary>
        /// A shortcut method marrying .NET Framework's ObjectCache to CacheAspect.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="cachePolicyTemplate">.NET Framework object driving cached item expiration.</param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public static CacheAspect<ICacheProvider> CreateCacheAspect(this ObjectCache cache, CacheItemPolicy cachePolicyTemplate, string regionName = null)
        {
            return cache.CreateCacheProvider(cachePolicyTemplate, regionName).CreateCacheAspect();
        }
    }
}
