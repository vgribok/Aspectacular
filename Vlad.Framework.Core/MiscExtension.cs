using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
{
    public static class MiscExtension
    {
        static readonly string[][] simpleParmTypeNames = 
        {
            new string[] { "Void", "void" },
            new string[] { "String", "string" },
            new string[] { "Byte", "byte" },
            new string[] { "SByte", "sbyte" },
            new string[] { "Int16", "short" },
            new string[] { "UInt16", "ushort" },
            new string[] { "Int32", "int" },
            new string[] { "UInt32", "int" },
            new string[] { "Int64", "long" },
            new string[] { "UInt64", "ulong" },
            new string[] { "Boolean", "bool" },
            new string[] { "Decimal", "decimal" },
            new string[] { "Double", "double" },
            new string[] { "Single", "float" },
        };

        static readonly Dictionary<string, string> stdTypeCSharpNames = new Dictionary<string, string>();

        static MiscExtension()
        {
            foreach (string[] pair in simpleParmTypeNames)
                stdTypeCSharpNames.Add(pair[0], pair[1]);
        }

        public static string FormatCSharp(this Type type)
        {
            string typeName = type.Name.TrimEnd('&');
            if (!stdTypeCSharpNames.TryGetValue(typeName, out typeName))
                typeName = type.Name;

            return typeName;
        }

        public static string FormatCSharpType(this ParameterInfo parmInfo)
        {
            string refOrOut = string.Empty;

            bool isRef = parmInfo.ParameterType.Name.EndsWith("&");

            if (isRef)
                refOrOut = parmInfo.IsOut ? "out " : "ref ";

            return string.Format("{0}{1}", refOrOut, parmInfo.ParameterType.FormatCSharp());
        }
    }
}
