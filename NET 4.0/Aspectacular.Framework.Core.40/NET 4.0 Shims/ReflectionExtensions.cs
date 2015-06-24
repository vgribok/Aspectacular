using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Aspectacular
{
    /// <summary>
    /// .NET 4.0 Framework compatibility shim.
    /// </summary>
    public static class ReflectionExtensions
    {
        public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element, bool inherit) where T : Attribute
        {
            return element.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        public static T GetCustomAttribute<T>(this ParameterInfo element, bool inherit = false) where T : Attribute
        {
            return element.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        public static object GetValue(this PropertyInfo propInfo, object obj)
        {
            return propInfo.GetValue(obj, index: null);
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MethodInfo methodInfo, bool inherit = false) where T : Attribute
        {
            return methodInfo.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        public static T GetCustomAttribute<T>(this MethodInfo methodInfo, bool inherit = false) where T : Attribute
        {
            return methodInfo.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), inherit).Cast<T>();
        }

        public static T GetCustomAttribute<T>(this Type type, bool inherit = false) where T : Attribute
        {
            return type.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }
    }
}
