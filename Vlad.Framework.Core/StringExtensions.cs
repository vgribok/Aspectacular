using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
{
    public static class StringExtensions
    {
        /// <summary>
        /// Same as Object.ToString(), but supports null string too.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nullObjString"></param>
        /// <returns></returns>
        public static string ToStringEx(this object obj, string nullObjString = "")
        {
            return obj == null ? (nullObjString ?? string.Empty) : obj.ToString();
        }
    }
}
