using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;

namespace Aspectacular
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Converts request parameter (querystring, form field, cookie value) 
        /// to a strongly-typed type.
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
            if (lazy == null)
            {
                lazy = new Lazy<T>(() => converter(request.Params[paramName]));
                HttpContext.Current.Items[key] = lazy;
            }

            return lazy.Value;
        }

        /// <summary>
        /// Converts request parameter (querystring, form field, cookie value) 
        /// to a strongly-typed value.
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
