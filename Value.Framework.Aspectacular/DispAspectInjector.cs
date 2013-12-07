using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{
    public class AllocateRunDisposeProxy<TDispClass> : InstanceProxy<TDispClass> 
        where TDispClass : class, IDisposable, new()
    {
        private static TDispClass Instantiate()
        {
            TDispClass instance = new TDispClass();
            return instance;
        }

        private static void Cleanup(TDispClass instance)
        {
            if (instance != null)
                instance.Dispose();
        }

        public AllocateRunDisposeProxy(params Aspect[] aspects)
            : base(Instantiate, Cleanup, aspects)
        {
        }
    }

    public static partial class AOP
    {
        /// <summary>
        /// Returns AOP proxy for TDispClass class derived from IDisposable.
        /// The proxy will instantiate the TDispClass object before the intercepted method call,
        /// and dispose of it after the intercepted method call.
        /// </summary>
        /// <typeparam name="TDispClass"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static AllocateRunDisposeProxy<TDispClass> GetAllocDisposeProxy<TDispClass>(params Aspect[] aspects)
            where TDispClass : class, IDisposable, new()
        {
            var proxy = new AllocateRunDisposeProxy<TDispClass>(aspects);
            return proxy;
        }
    }
}
