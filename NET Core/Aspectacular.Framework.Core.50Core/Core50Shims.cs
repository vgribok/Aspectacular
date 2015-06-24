using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    /// <summary>
    /// Extensions for crucial methods messing in .NET Core framework
    /// </summary>
    public static class Core50Shims
    {
        public static bool IsNullOrEmpty(this string str)
        {
            // ReSharper disable once ReplaceWithStringIsNullOrEmpty
            return str == null || str.Length == 0;
        }
    }
}
