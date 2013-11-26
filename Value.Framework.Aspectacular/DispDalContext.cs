using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{
    public class AllocateRunDisposeContext<TDispClass> : DalContext<TDispClass> 
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

        public AllocateRunDisposeContext(params Aspect[] aspects)
            : base(Instantiate, Cleanup, aspects)
        {
        }
    }

    public static partial class AOP
    {
        public static TOut AllocRunDispose<TInstance, TOut>(Func<TInstance, Expression<Func<TOut>>> proxy, params Aspect[] aspects)
            where TInstance : class, IDisposable, new()
        {
            var context = new AllocateRunDisposeContext<TInstance>(aspects);
            TOut retVal = context.Execute<TOut>(proxy);
            return retVal;
        }
    }
}
