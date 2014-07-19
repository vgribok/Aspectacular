#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Collections.Concurrent;

namespace Aspectacular.Test
{
    public class StupidSimpleInProcCache : ICacheProvider
    {
        private readonly ConcurrentDictionary<string, object> cache = new ConcurrentDictionary<string, object>();

        public void Set(string key, object val)
        {
            this.cache[key] = val;
        }

        public bool TryGet(string key, out object val)
        {
            return this.cache.TryGetValue(key, out val);
        }
    }
}