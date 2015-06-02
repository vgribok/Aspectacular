#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Web;

namespace Aspectacular
{
    public static class RequestExtensions
    {
        /// <summary>
        ///     Converts request parameter (querystring, form field, cookie value)
        ///     to a strongly-typed type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static T GetParam<T>(this HttpRequest request, string paramName, Func<string, T> converter)
        {
            string key = "VH_Lazy_{0}".SmartFormat(paramName);
            Lazy<T> lazy = HttpContext.Current.Items[key] as Lazy<T>;
            if(lazy == null)
            {
                lazy = new Lazy<T>(() => converter(request.Params[paramName]));
                HttpContext.Current.Items[key] = lazy;
            }

            return lazy.Value;
        }

        /// <summary>
        ///     Converts request parameter (querystring, form field, cookie value)
        ///     to a strongly-typed value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static T? GetParam<T>(this HttpRequest request, string paramName, Func<string, T?> converter) where T : struct
        {
            return request.GetParam<T?>(paramName, val => val == null ? (T?)null : converter(val));
        }
    }
}