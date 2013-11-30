using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{
    public class AllocateRunDisposeInterceptor<TDispClass> : InstanceInterceptor<TDispClass> 
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

        public AllocateRunDisposeInterceptor(params Aspect[] aspects)
            : base(Instantiate, Cleanup, aspects)
        {
        }
    }

    public static partial class AOP
    {
        /// <summary>
        /// Instantiates object, runs its instance method *returning TOut* result, and disposes of the instance.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public static TOut AllocRunDispose<TInstance, TOut>(Aspect[] aspects, Func<TInstance, Expression<Func<TOut>>> proxy)
            where TInstance : class, IDisposable, new()
        {
            var interceptor = new AllocateRunDisposeInterceptor<TInstance>(aspects);
            TOut retVal = interceptor.Invoke<TOut>(proxy);
            return retVal;
        }

        /// <summary>
        /// Instantiates object, runs its instance method *returning nothing", and disposes of the instance.
        /// </summary>
        /// <typeparam name="TInstance"></typeparam>
        /// <param name="aspects"></param>
        /// <param name="proxy"></param>
        public static void AllocRunDispose<TInstance>(Aspect[] aspects, Func<TInstance, Expression<Action>> proxy)
            where TInstance : class, IDisposable, new()
        {
            var interceptor = new AllocateRunDisposeInterceptor<TInstance>(aspects);
            interceptor.Invoke(proxy);
        }
    }
}
