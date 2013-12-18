using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
{
    public enum ParamDirectionEnum
    {
        In = 1, Out, RefInOut
    }


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
            new string[] { "Char", "char" },
        };

        static readonly Dictionary<string, string> stdTypeCSharpNames = new Dictionary<string, string>();

        static MiscExtension()
        {
            foreach (string[] pair in simpleParmTypeNames)
                stdTypeCSharpNames.Add(pair[0], pair[1]);
        }

        public static bool IsSimpleCSharpType(this Type type)
        {
            string typeName = type.Name.TrimEnd('&');
            return stdTypeCSharpNames.ContainsKey(typeName);
        }

        public static string FormatCSharp(this Type type, bool fullyQualified = false)
        {
            string typeName = TypeToCSharpString(type, fullyQualified);

            string stdName;
            if (stdTypeCSharpNames.TryGetValue(typeName, out stdName))
                typeName = stdName;

            return typeName;
        }

        private static string TypeToCSharpString(Type type, bool fullyQualified = false)
        {
            string typeName = (fullyQualified ? type.FullName : type.Name).TrimEnd('&');

            if (!type.IsGenericType)
                return typeName;

            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingType = type.GetGenericArguments()[0];
                return string.Format("{0}?", TypeToCSharpString(underlyingType));
            }
            string baseName = typeName.Substring(0, typeName.IndexOf("`"));
            string generic = string.Join(", ", type.GetGenericArguments().Select(paramType => TypeToCSharpString(paramType, fullyQualified)));
            string fullName = string.Format("{0}<{1}>", baseName, generic);
            return fullName;
        }

        public static ParamDirectionEnum GetDirection(this ParameterInfo parmInfo)
        {
            bool isRef = parmInfo.ParameterType.Name.EndsWith("&");

            if (isRef)
                return parmInfo.IsOut ? ParamDirectionEnum.Out : ParamDirectionEnum.RefInOut;

            return ParamDirectionEnum.In;
        }

        public static string FormatCSharpType(this ParameterInfo parmInfo, bool fullyQualified = false)
        {
            string refOrOut;

            switch(parmInfo.GetDirection())
            {
                case ParamDirectionEnum.Out:
                    refOrOut = "out ";
                    break;
                case ParamDirectionEnum.RefInOut:
                    refOrOut = "ref ";
                    break;
                default:
                    refOrOut = string.Empty;
                    break;
            }

            return string.Format("{0}{1}", refOrOut, parmInfo.ParameterType.FormatCSharp(fullyQualified));
        }


    }
}
