#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Runtime.Caching;

// ReSharper disable CSharpWarnings::CS0618
namespace Aspectacular
{
    /// <summary>
    ///     Implementing this minimal-dependency interface is
    ///     all that's needed to enable caching via CacheAspect.
    /// </summary>
    /// <remarks>
    /// This interface does not prescribe any expiration policy or functionality.
    /// Expiration/Removal items from cache is up to cache implementation.
    /// </remarks>
    public interface ICacheProvider2
    {
        /// <summary>
        /// Implement to save a value to a cache store.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="val">Value to cache.</param>
        /// <param name="proxy">AOP proxy for additional context.</param>
        void Set(string key, object val, Proxy proxy);

        /// <summary>
        /// Implement to retrieve values from cache.
        /// Must return true if item found in cache, false otherwise.
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="val">Returned item if found in cache.</param>
        /// <param name="proxy">AOP proxy reference for additional context.</param>
        /// <returns></returns>
        bool TryGet(string key, out object val, Proxy proxy);
    }

    /// <summary>
    ///     Implementing this minimal-dependency interface is
    ///     all that's needed to enable caching via CacheAspect.
    /// </summary>
    [Obsolete("Use ICacheProvider2 instead.")]
    public interface ICacheProvider
    {
        void Set(string key, object val);
        bool TryGet(string key, out object val);
    }

    /// <summary>
    ///     Note: Building method caching key is very expensive performance-wise.
    ///     Use it only to cache data that store in slow storage types, like databases, files, on the network, on the Internet,
    ///     etc.
    /// </summary>
    /// <remarks>
    ///     Please note that CacheAspect will not attempt to invalidate the cache - it's a responsibility of the ICacheProvider
    ///     implementation.
    /// </remarks>
    public class CacheAspect : Aspect
    {
        protected ICacheProvider Cache { get; private set; }
        protected ICacheProvider2 Cache2 { get; private set; }

        /// <summary>
        /// Returns true if value was found in cache.
        /// It's set only after intercepted method was called.
        /// </summary>
        public bool ValueFoundInCache { get; private set; }

        [Obsolete("Use CacheFactory.CreateCacheAspect() instead.")]
        protected internal CacheAspect(ICacheProvider cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            this.Cache = cacheProvider;
        }

        [Obsolete("Use CacheFactory.CreateCacheAspect() instead.")]
        protected internal CacheAspect(ICacheProvider2 cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            this.Cache2 = cacheProvider;
        }

        public override void Step_2_BeforeTryingMethodExec()
        {
            this.LogInformationWithKey("Cache provider type", ((object)this.Cache2 ?? this.Cache).GetType().FormatCSharp());
            this.LogInformationData("Method is cacheable", this.Proxy.CanCacheReturnedResult);

            if(!this.Proxy.CanCacheReturnedResult)
                return;

            this.LogParametersWithValues();

            this.GetValueFromCacheIfItsThere();
        }

        private void LogParametersWithValues()
        {
            foreach(InterceptedMethodParamMetadata paramInfo in this.Proxy.InterceptedCallMetaData.Params)
            {
                this.LogInformationWithKey("Parameter \"{0}\"".SmartFormat(paramInfo.Name),
                    "Type: [{0}], Value: [{1}]", paramInfo.Type.FormatCSharp(), paramInfo.FormatSlowEvaluatingValue(false));
            }
        }

        public override void Step_5_FinallyAfterMethodExecution(bool interceptedCallSucceeded)
        {
            if(!this.Proxy.CanCacheReturnedResult)
                return;

            if(this.ValueFoundInCache)
                return;

            this.SaveValueToCache();
        }

        private void SaveValueToCache()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();
            object val = this.Proxy.InterceptedMedthodCallFailed ? this.Proxy.MethodExecutionException : this.Proxy.ReturnedValue;
            
            if(this.Cache2 != null)
                this.Cache2.Set(cacheKey, val, this.Proxy);
            else
                this.Cache.Set(cacheKey, val);
        }

        private void GetValueFromCacheIfItsThere()
        {
            string cacheKey = this.BuildMethodCacheKeyVerySlowly();

            object cachedValue;

            if (this.Cache2 != null)
                this.ValueFoundInCache = this.Cache2.TryGet(cacheKey, out cachedValue, this.Proxy);
            else
                this.ValueFoundInCache = this.Cache.TryGet(cacheKey, out cachedValue);

            this.LogInformationData("Found in cache", this.ValueFoundInCache);

            if(this.ValueFoundInCache)
            {
                if(cachedValue is Exception)
                    this.Proxy.MethodExecutionException = cachedValue as Exception;
                else
                    this.Proxy.ReturnedValue = cachedValue;

                this.Proxy.CancelInterceptedMethodCall = true;
            }
        }

        protected string BuildMethodCacheKeyVerySlowly()
        {
            string mainMethodSignature = this.Proxy.InterceptedCallMetaData.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);
            if(this.Proxy.PostProcessingCallMetadata == null)
                return mainMethodSignature;

            string prostProcessingMethodSignature = this.Proxy.PostProcessingCallMetadata.GetMethodSignature(ParamValueOutputOptions.SlowInternalValue);
            string combined = string.Format("{0}+{1}", mainMethodSignature, prostProcessingMethodSignature);

            return combined;
        }
    }

    /// <summary>
    ///     A class fronting .NET Framework's mediocre ObjectCache
    ///     with Aspectacular-friendly ICacheProvider implementation.
    /// </summary>
    public class ObjectCacheFacade : ICacheProvider2
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

        public virtual void Set(string key, object val, Proxy proxy)
        {
            this.objectCache.Add(key, val, this.ClonePolicy(), this.regionName);
        }

        public virtual bool TryGet(string key, out object val, Proxy proxy)
        {
            // It really stinks that MS folks who designed ObjectCache didn't think of "bool TryGetValue(key, out val)" pattern
            // so this thing could be done in one search instead of two. Terrible!

            if(!this.objectCache.Contains(key, this.regionName))
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
        ///     Instantiates new CacheAspect for a given cache provider.
        /// </summary>
        /// <param name="cacheProvider"></param>
        /// <returns></returns>
        public static CacheAspect CreateCacheAspect(this ICacheProvider2 cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            var cacheAspect = new CacheAspect(cacheProvider);
            return cacheAspect;
        }

        /// <summary>
        ///     Instantiates new CacheAspect for a given cache provider.
        /// </summary>
        /// <param name="cacheProvider"></param>
        /// <returns></returns>
        [Obsolete("Use CreateCacheAspect(ICacheProvider2) instead.")]
        public static CacheAspect CreateCacheAspect(this ICacheProvider cacheProvider)
        {
            if (cacheProvider == null)
                throw new ArgumentNullException("cacheProvider");

            var cacheAspect = new CacheAspect(cacheProvider);
            return cacheAspect;
        }
        /// <summary>
        ///     Augments .NET Framework ObjectCache and its descendants into
        ///     Aspectacular-friendly cache provider compatible with the CacheAspect.
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
        ///     A shortcut method marrying .NET Framework's ObjectCache to CacheAspect.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="cachePolicyTemplate">.NET Framework object driving cached item expiration.</param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public static CacheAspect CreateCacheAspect(this ObjectCache cache, CacheItemPolicy cachePolicyTemplate, string regionName = null)
        {
            return cache.CreateCacheProvider(cachePolicyTemplate, regionName).CreateCacheAspect();
        }
    }
}
// ReSharper restore CSharpWarnings::CS0618
