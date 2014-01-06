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
        /// <summary>
        /// A pass-through constructor that creates proxy which does neither instantiate nor cleans up the instance after it's used.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        public AllocateRunDisposeProxy(TDispClass instance, IEnumerable<Aspect> aspects)
            : base(instance, aspects)
        {
        }

        /// <summary>
        /// Creates proxy that instantiates IDisposable class
        /// and after method invocation calls class's Dispose().
        /// </summary>
        /// <param name="aspects"></param>
        public AllocateRunDisposeProxy(IEnumerable<Aspect> aspects)
            : base(Instantiate, Cleanup, aspects)
        {
        }

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
        public static AllocateRunDisposeProxy<TDispClass> GetAllocDisposeProxy<TDispClass>(IEnumerable<Aspect> aspects)
            where TDispClass : class, IDisposable, new()
        {
            var proxy = new AllocateRunDisposeProxy<TDispClass>(aspects);
            return proxy;
        }
    }
}
