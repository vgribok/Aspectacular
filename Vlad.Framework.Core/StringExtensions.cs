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

        /// <summary>
        /// Same as string.IsNullOrWhiteSpace(str), but with more expressive and fluent syntax. Won't trip on this=null.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBlank(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        /// When args are not supplied, format string curly braces '{' and '}' are left unchanged.
        /// Allows "some format {0}}.SmartFormat(myData);" syntax.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string SmartFormat(this string format, params object[] args)
        {
            if (args.IsNullOrEmpty())
                return format;

            return string.Format(format, args);
        }
    }
}
