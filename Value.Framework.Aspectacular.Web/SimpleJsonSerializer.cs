using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Value.Framework.Aspectacular.Web
{
    public static class SimpleJsonSerializer
    {
        public static JavaScriptSerializer JsSerializer
        {
            get { return new JavaScriptSerializer(); }
        }

        public static string ToJsonString(this object obj)
        {
            return JsSerializer.Serialize(obj);
        }

        public static object ToJsonObject(this string jsonStr)
        {
            return JsSerializer.DeserializeObject(jsonStr);
        }

        public static T FromJsonString<T>(this string jsonStr)
        {
            return JsSerializer.Deserialize<T>(jsonStr);
        }

        public static T FromJsonObject<T>(this object obj)
        {
            return obj.ToJsonString().FromJsonString<T>();
        }

        public static object FromJsonString(this string jsonStr, Type type)
        {
            return JsSerializer.Deserialize(jsonStr, type);
        }
    }
}
