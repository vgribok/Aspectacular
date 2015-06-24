#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Aspectacular
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Same as Object.ToString(), but supports null string too.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nullObjString"></param>
        /// <returns></returns>
        public static string ToStringEx(this object obj, string nullObjString = null)
        {
            return obj == null ? nullObjString : obj.ToString();
        }

        /// <summary>
        ///     Same as string.IsNullOrWhiteSpace(str), but with more expressive and fluent syntax. Won't trip on this=null.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBlank(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        /// <summary>
        ///     When args are not supplied, format string curly braces '{' and '}' are left unchanged.
        ///     Allows "Some format string {0}".SmartFormat(myData); syntax.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string SmartFormat(this string format, params object[] args)
        {
            if(format == null || args == null || args.Length == 0)
                return format;

            return string.Format(format, args);
        }

        #region Value type parsing

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static TEnum Parse<TEnum>(this string str, TEnum defaultValue) where TEnum : struct
        {
            TEnum val;
            if(Enum.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        [Obsolete("Use str.ParseEnum<TEnum>() instead.")]
        public static TEnum? Parse<TEnum>(this string str) where TEnum : struct
        {
            TEnum val;
            if(Enum.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="str"></param>
        /// <returns></returns>
        public static TEnum? ParseEnum<TEnum>(this string str) where TEnum : struct
        {
            TEnum val;
            if (Enum.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte? ParseByte(this string str)
        {
            byte val;
            if(byte.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static byte ParseByte(this string str, byte defaultValue)
        {
            byte val;
            if(byte.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int? ParseInt(this string str)
        {
            int val;
            if(int.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int Parse(this string str, int defaultValue)
        {
            int val;
            if(int.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long? ParseLong(this string str)
        {
            long val;
            if(long.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static long Parse(this string str, long defaultValue)
        {
            long val;
            if(long.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool? ParseBool(this string str)
        {
            bool val;
            if(bool.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static bool Parse(this string str, bool defaultValue)
        {
            bool val;
            if(bool.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static decimal? ParseDecimal(this string str)
        {
            decimal val;
            if(decimal.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal Parse(this string str, decimal defaultValue)
        {
            decimal val;
            if(decimal.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double? ParseDouble(this string str)
        {
            double val;
            if(double.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double Parse(this string str, double defaultValue)
        {
            double val;
            if(double.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        /// <summary>
        ///     Parses string and returns null if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Guid? ParseGuid(this string str)
        {
            Guid val;
            if(Guid.TryParse(str, out val))
                return val;

            return null;
        }

        /// <summary>
        ///     Parses string and returns default value if parsing failed.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static Guid Parse(this string str, Guid defaultValue)
        {
            Guid val;
            if(Guid.TryParse(str, out val))
                return val;

            return defaultValue;
        }

        #endregion Value type parsing

        #region Improved upper/lower case conversion methods

        /// <summary>
        ///     Better version of string.ToLower() -
        ///     handles null properly. Uses user's UI culture if optional culture is not specified.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="optionalCultureInfo">Optional CultureInfo. Users user's UI culture if not specified.</param>
        /// <returns></returns>
        public static string ToLowerEx(this string str, CultureInfo optionalCultureInfo = null)
        {
            if(str == null)
                return null;

#if !CORE
            str = str.ToLower(optionalCultureInfo ?? CultureInfo.InstalledUICulture);
#else
            str = str.ToLower();
#endif

            return str;
        }

        /// <summary>
        ///     Better version of string.ToLowerInvariant() - handles null properly.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToLowerInvariantEx(this string str)
        {
            return str.ToLowerEx(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Better version of string.ToUpper() -
        ///     handles null properly. Uses user's UI culture if optional culture is not specified.
        /// </summary>
        /// <param name="str"></param>
#if !CORE
        /// <param name="optionalCultureInfo">Optional CultureInfo. Users user's UI culture if not specified.</param>
#endif
        /// <returns></returns>
        public static string ToUpperEx(this string str
#if !CORE
            , CultureInfo optionalCultureInfo = null
#endif
            )
        {
            if(str == null)
                return null;

#if CORE
            str = str.ToUpper();
#else
            str = str.ToUpper(optionalCultureInfo ?? CultureInfo.InstalledUICulture);
#endif

            return str;
        }

        /// <summary>
        ///     Better version of string.ToUpperInvariant() - handles null properly.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToUpperInvariantEx(this string str)
        {
#if CORE
            return str.ToUpperEx();
#else
            return str.ToUpperEx(CultureInfo.InvariantCulture);
#endif
        }

        #endregion Improved upper/lower case conversion methods

        /// <summary>
        ///     Simple, exception-free way of getting regular expression group value.
        ///     Returns null if group is not found.
        /// </summary>
        /// <param name="rexParsingResult"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static string GetGroupValue(this Match rexParsingResult, string groupName)
        {
            if(rexParsingResult == null)
                return null;

            Group group = rexParsingResult.Groups[groupName];
            if(group == null || group.Length == 0 || !group.Success)
                return null;

            return group.Value;
        }

        public static string TruncateIfExceeds(this string str, uint maxLen, string optionalEllipsis = "...")
        {
            if(str == null || str.Length <= maxLen)
                return str;

            NonNullString ellipsis = optionalEllipsis;
            if(ellipsis.String.Length > maxLen)
                ellipsis = (string)null;

// ReSharper disable once PossibleNullReferenceException
            string truncated = str.Substring(0, (int)maxLen - ellipsis.String.Length);
            truncated = string.Format("{0}{1}", truncated, ellipsis);

            return truncated;
        }

        /// <summary>
        ///     Concatenates [str] given number of times.
        ///     Returns null if count is less than 1.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string Repeat(this string str, int count)
        {
            if(count < 1)
                return null;

            if(count == 1 || string.IsNullOrEmpty(str))
                return str;

            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < count; i++)
                sb.Append(str);

            return sb.ToString();
        }

        /// <summary>
        ///     Returns str's part to the left of separator, or null is separator not found.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string LeftOf(this string str, string separator)
        {
            if(string.IsNullOrEmpty(separator))
                return str;

            if(string.IsNullOrEmpty(str))
                return null;

            // ReSharper disable once StringIndexOfIsCultureSpecific.1
            int sepIndex = str.IndexOf(separator);
            if(sepIndex < 0)
                return null;

            return str.Substring(0, sepIndex);
        }

        /// <summary>
        ///     Returns str's part to the right of the last instance of separator, or null is separator not found.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string RightOf(this string str, string separator)
        {
            if(string.IsNullOrEmpty(separator))
                return str;

            if(string.IsNullOrEmpty(str))
                return null;

            // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
            int sepIndex = str.LastIndexOf(separator);
            if(sepIndex < 0)
                return null;

            sepIndex++;
            if(sepIndex > str.Length)
                return null;

            if(sepIndex == str.Length)
                return string.Empty;

            return str.Substring(sepIndex);
        }
    }
}