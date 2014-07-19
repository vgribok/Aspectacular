#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Web.Script.Serialization;

namespace Aspectacular
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

        public static T FromJsonString<T>(this string jsonStr) //where T : new()
        {
            return JsSerializer.Deserialize<T>(jsonStr);
        }

        public static T FromJsonObject<T>(this object obj) //where T : new()
        {
            return obj.ToJsonString().FromJsonString<T>();
        }

        public static object FromJsonString(this string jsonStr, Type type)
        {
            return JsSerializer.Deserialize(jsonStr, type);
        }
    }
}