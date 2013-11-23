using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{
    public class DalContext
    {
        private LambdaExpression methodExp { get; set; }
        private Delegate blMethod;

        /// <summary>
        /// Core idea behind the interception strategy.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="proxy"></param>
        /// <returns></returns>
        public TOut Execute<TOut>(Func<DalContext, Expression<Func<TOut>>> proxy)
        {
            Expression<Func<TOut>> blExp = proxy(this);
            this.methodExp = blExp;

            Func<TOut> blDelegate = blExp.Compile();
            this.blMethod = blDelegate;

            TOut retVal = blDelegate.Invoke();

            return retVal;
        }

        public string FakeBlMethod(int id)
        {
            return id.ToString();
        }
    }
}
