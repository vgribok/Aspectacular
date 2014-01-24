using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    public static class DisposeAfter 
    {
        /// <summary>
        /// Instantiates instance, executes and calls Dispose() in one shot.
        /// </summary>
        /// <typeparam name="TDisp">Disposable type to create instance of.</typeparam>
        /// <typeparam name="T">Function return result type.</typeparam>
        /// <param name="func">Function to call</param>
        /// <returns></returns>
        public static T Execute<TDisp, T>(Func<TDisp, T> func)
            where TDisp : IDisposable, new()
        {
            using (var disp = new TDisp())
                return func(disp);
        }

        /// <summary>
        /// Instantiates instance, executes and calls Dispose() in one shot.
        /// </summary>
        /// <typeparam name="TDisp">Disposable type to create instance of.</typeparam>
        /// <param name="func">Function to call.</param>
        public static void Execute<TDisp>(Action<TDisp> func)
            where TDisp : IDisposable, new()
        {
            using (var disp = new TDisp())
                func(disp);
        }
    }
}
