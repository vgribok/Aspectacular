#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Aspectacular
{
    public static class QueryStringEx
    {
        /// <summary>
        ///     Changes query string of current http request by adding or replacing query string parameter value
        ///     in the way that avoids creating duplicates.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <param name="paramValueFormat"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static void SetQsValue(this HttpRequest request, string paramName, string paramValueFormat, params object[] args)
        {
            NameValueCollection qs = EnsureQsStored(request);
            qs[paramName] = paramValueFormat.SmartFormat(args);
        }

        //public static string GetQsValue(this HttpRequest request, string paramName)
        //{
        //    NameValueCollection qs = EnsureQsStored(request);
        //    return qs[paramName];
        //}

        //public static string GetQsValue(this HttpRequest request, int paramIndex)
        //{
        //    NameValueCollection qs = EnsureQsStored(request);
        //    return qs[paramIndex];
        //}

        private static NameValueCollection EnsureQsStored(HttpRequest request)
        {
            if(!HttpContext.Current.Items.Contains("QueryString"))
                HttpContext.Current.Items["QueryString"] = new NameValueCollection(request.QueryString);

            NameValueCollection qs = (NameValueCollection)HttpContext.Current.Items["QueryString"];
            return qs;
        }

        /// <summary>
        ///     Returns modified http request querystring, starting with question mark,
        ///     or "" if not query string parameters were specified.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetQs(this HttpRequest request)
        {
            NameValueCollection qs = EnsureQsStored(request);

            StringBuilder sb = new StringBuilder();

            for(int i = 0; i < qs.Count; i++)
            {
                string key = qs.GetKey(i);
// ReSharper disable once AssignNullToNotNullAttribute
                string val = string.Join("&", qs.GetValues(i));
                string pair = key == null ? val : string.Format("{0}={1}", HttpUtility.UrlEncode(qs.GetKey(i)), HttpUtility.UrlEncode(qs[i]));

                sb.Append(sb.Length == 0 ? "?" : "&");
                sb.Append(pair);
            }

            return sb.ToString();
        }

        /// <summary>
        ///     Returns redirect URL with modified http query string
        /// </summary>
        /// <param name="request"></param>
        /// <param name="includeHost">if true, returns url starting with "http[s]://..". If false, returns url starting with "/".</param>
        /// <returns></returns>
        public static string GetUrlWithQs(this HttpRequest request, bool includeHost = false)
        {
            string qs = request.GetQs();

            string url = includeHost ? request.Url.GetLeftPart(UriPartial.Path) : request.Url.AbsolutePath;
            url += qs;
            return url;
        }
    }
}