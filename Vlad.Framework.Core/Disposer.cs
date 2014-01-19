using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    public class DisposeAfter<TDisp> where TDisp : IDisposable, new()
    {
        public static T Execute<T>(Func<TDisp, T> func)
        {
            using (var disp = new TDisp())
                return func(disp);
        }

        public static void Execute(Action<TDisp> func)
        {
            using (var disp = new TDisp())
                func(disp);
        }
    }
}
