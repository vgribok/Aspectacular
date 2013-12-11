using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Value.Framework.Aspectacular.Aspects;

namespace Value.Framework.UnitTests
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
