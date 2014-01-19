using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
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

        #region Value type parsing

        /// <summary>
        /// Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte? ParseByte(this string str)
        {
            byte val;
            if (byte.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        /// Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static byte ParseByte(this string str, byte defaultValue)
        {
            byte val;
            if (byte.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        /// Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int? ParseInt(this string str)
        {
            int val;
            if (int.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        /// Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int Parse(this string str, int defaultValue)
        {
            int val;
            if (int.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        /// Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool? ParseBool(this string str)
        {
            bool val;
            if (bool.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        /// Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool Parse(this string str, bool defaultValue)
        {
            bool val;
            if (bool.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        /// Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static decimal? ParseDecimal(this string str)
        {
            decimal val;
            if (decimal.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        /// Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal Parse(this string str, decimal defaultValue)
        {
            decimal val;
            if (decimal.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        /// Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double? ParseDouble(this string str)
        {
            double val;
            if (double.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        /// Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double Parse(this string str, double defaultValue)
        {
            double val;
            if (double.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        #endregion Value type parsing
    }
}
