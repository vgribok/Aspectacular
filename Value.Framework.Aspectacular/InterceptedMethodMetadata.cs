using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Value.Framework.Core;

namespace Value.Framework.Aspectacular
{
    public class SecretParamValueAttribute : System.Attribute
    {
    }

    public enum ParamValueOutputOptions
    {
        /// <summary>
        /// Value not to be shown for parameter
        /// </summary>
        NoValue, 
        
        /// <summary>
        /// Value to be presented to the user.
        /// Use sparingly as first evaluation of every value is very slow (Expression.Compile() + Reflection.Invoke()-slow).
        /// </summary>
        SlowUIValue, 
        
        /// <summary>
        /// Value to be used internally for method result caching.
        /// Use sparingly as first evaluation of every value is very slow (Reflection-slow).
        /// </summary>
        SlowInternalValue,
    }

    /// <summary>
    /// Class substituting secret parameter values.
    /// </summary>
    internal class SecretValueHash
    {
        internal readonly string ValHash;

        public SecretValueHash(object val)
        {
            if (val == null)
                this.ValHash = null;
            else
            {
                byte[] stringBytes = System.Text.ASCIIEncoding.Unicode.GetBytes(val.ToString());
                long crc = ComputeCRC(stringBytes);
                this.ValHash = string.Format("{0:X}-{1:X}", val.GetHashCode(), crc);
            }
        }

        public static long ComputeCRC(byte[] val)
        {
            long crc;
            long q;
            byte c;
            crc = 0;
            for (int i = 0; i < val.Length; i++)
            {
                c = val[i];
                q = (crc ^ c) & 0x0f;
                crc = (crc >> 4) ^ (q * 0x1081);
                q = (crc ^ (c >> 4)) & 0xf;
                crc = (crc >> 4) ^ (q * 0x1081);
            }
            return crc;
        }

        public override string ToString()
        {
            return this.ValHash;
        }

        public override int GetHashCode()
        {
            return this.ValHash.GetHashCode();
        }
    }

    public class InterceptedMethodParamMetadata
    {
        private readonly Lazy<object> slowEvaluatingValueLoader;

        public readonly ParameterInfo ParamReflection;

        private readonly Expression expression;

        /// <summary>
        /// Function parameter name.
        /// </summary>
        public string Name { get; private set;  }
        
        /// <summary>
        /// Function parameter type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Parameter value retrieved via slow evaluation.
        /// Warning 1: getting this value is Expression.Compile()-and-Reflection.Invoke()-slow until value is cached.
        /// Warning 2: getting this value evaluates an input expression until value is cached. To avoid 
        /// unexpected behavior, parameter values should be invariant (having same outcome) no matter how many times parameters is evaluated.
        /// </summary>
        /// <remarks>
        /// If your call looks like:
        /// <code>
        /// obj.GetProxy(aspects).Invoke(inst => inst.Foo("hello!", CreateNewUserAndGetID("John Doe")));
        /// </code>
        /// please note that <code>CreateNewUserAndGetID("John Doe")</code> function will be called 
        /// not only when Foo() is called to pass the parameter, but also when this property value is retrieved.
        /// Therefore, if intercepted inputs should be invariant. So instead of calling
        /// <code>inst.Foo("hello!", CreateNewUserAndGetID("John Doe"))</code>, store value returned by
        /// <code>CreateNewUserAndGetID("John Doe")</code> and pass stored value as a parameter.
        /// </remarks>
        public object SlowEvaluatingValueLoader
        {
            get 
            { 
                return this.slowEvaluatingValueLoader.Value;
            }
        }

        public T GetCustomAttribute<T>(bool inherit = false) where T : System.Attribute
        {
            return this.ParamReflection.GetCustomAttribute<T>(inherit);
        }

        public IEnumerable<Attribute> GetCustomAttributes<T>(bool inherit = false) where T : System.Attribute
        {
            return this.ParamReflection.GetCustomAttributes<T>(inherit);
        }

        public bool HasAttribute<T>() where T : System.Attribute
        {
            return this.GetCustomAttributes<T>(inherit: false).Any();
        }

        public string GetSlowEvaluatingValueString(bool trueUI_falseInternal)
        {
            object val = this.SlowEvaluatingValueLoader;

            return FormatParamValue(this.Type, val, trueUI_falseInternal);
        }

        public static string FormatParamValue(Type type, object val, bool trueUI_falseInternal)
        {
            if (val == null)
                return "[null]";

            if (trueUI_falseInternal && val is SecretValueHash)
                return "[secret]";

            if (val is string)
                return string.Format("\"{0}\"", val);
            else if (val is char)
                return string.Format("'{0}'", val);

            string strVal = val.ToString();
            if (strVal == type.ToString())
                return trueUI_falseInternal ? "[no string value]" : string.Format("HASH:{0:X}", val.GetHashCode());

            return  type.IsSimpleCSharpType() ? strVal : string.Format("[{0}]", strVal);
        }

        public bool ValueIsSecret
        {
            get
            {
                return this.HasAttribute<SecretParamValueAttribute>();
            }
        }

        public override string ToString()
        {
            return this.ToString(ParamValueOutputOptions.NoValue);
        }

        public string ToString(ParamValueOutputOptions options = ParamValueOutputOptions.NoValue)
        {
            string paramStrValue = null;

            if(options != ParamValueOutputOptions.NoValue)
                paramStrValue = string.Format(" = {0}", this.GetSlowEvaluatingValueString(options == ParamValueOutputOptions.SlowUIValue));

            string paramString = string.Format("{0} {1}{2}", this.ParamReflection.FormatCSharpType(), this.Name, paramStrValue);
            return paramString;
        }

        public InterceptedMethodParamMetadata(ParameterInfo paramReflection, Expression paramExpression)
        {
            this.ParamReflection = paramReflection;
            this.Name = this.ParamReflection.Name;
            this.Type = this.ParamReflection.ParameterType;
            this.expression = MassageParamExpression(paramExpression);

            this.slowEvaluatingValueLoader = new Lazy<object>(() =>
            {
                object val = VerySlowlyCompileAndInvoke(this.expression);

                if (this.ValueIsSecret)
                    val = new SecretValueHash(val);

                return val;
            });
        }

        public static Expression MassageParamExpression(Expression paramExpression)
        {
            if (paramExpression is UnaryExpression)
                return MassageParamExpression((paramExpression as UnaryExpression).Operand);

            return paramExpression;
        }


        public static object VerySlowlyCompileAndInvoke(Expression expression)
        {
            object val = null;

            if (expression != null)
            {
                if (expression.NodeType == ExpressionType.Constant)
                    val = ((ConstantExpression)expression).Value;
                else
                    // This is really, veeery, terribly slow. 
                    // The performance loss double-whammy is expression compilation plus reflection invocation.
                    val = Expression.Lambda(expression).Compile().DynamicInvoke();
            }
            return val;
        }

    }

    public class InterceptedMethodMetadata
    {
        protected MethodCallExpression interceptedMethodExpression;
        public MethodInfo MethodReflectionInfo { get; private set; }
        public IEnumerable<Attribute> MethodAttributes { get { return this.MethodReflectionInfo.GetCustomAttributes(); } }
        private readonly object augmentedInstance;
        public readonly List<InterceptedMethodParamMetadata> Params = new List<InterceptedMethodParamMetadata>();


        private readonly Lazy<object> thisObjectSlowEvaluator = null;

        public InterceptedMethodMetadata(object augmentedInstance, LambdaExpression callLambdaExp)
        {
            try
            {
                this.interceptedMethodExpression = (MethodCallExpression)callLambdaExp.Body;
            }
            catch (Exception ex)
            {
                string errorText = "Intercepted method expression must be a function call. \"{0}\" is invalid in this context."
                                        .SmartFormat(callLambdaExp.Body);
                throw new ArgumentException(errorText, ex);
            }

            this.augmentedInstance = augmentedInstance;
            this.MethodReflectionInfo = this.interceptedMethodExpression.Method;

            this.InitParameterMetadata();
        }

        private void InitParameterMetadata()
        {
            ParameterInfo[] paramData = this.MethodReflectionInfo.GetParameters();

            for (int i = 0; i < paramData.Length; i++)
            {
                Expression paramExpression = this.interceptedMethodExpression.Arguments[i];

                var paramMetadata = new InterceptedMethodParamMetadata(paramData[i], paramExpression);
                this.Params.Add(paramMetadata);
            }
        }

        public object SlowEvaluateThisObject()
        {
            if (this.IsStaticMethod || this.thisObjectSlowEvaluator == null)
                return null;

            return this.thisObjectSlowEvaluator.Value;
        }

        public bool IsStaticMethod
        {
            get 
            { 
                return (this.MethodReflectionInfo.Attributes & System.Reflection.MethodAttributes.Static) != 0;
                //return this.interceptedMethodExpression.Object == null; 
            }
        }

        public string GetMethodSignature(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            string signature = string.Format("{0} {1}({2})",
                this.MethodReflectionInfo.ReturnType.FormatCSharp(),
                this.FormatMethodName(valueOutputOptions),
                this.FormatMethodParameters(valueOutputOptions)
                );

            return signature;
        }

        private string FormatMethodName(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            return string.Format("{0}{1}.{2}",
                this.IsStaticMethod ? "static " : this.FormatThisValue(valueOutputOptions),
                    this.MethodReflectionInfo.DeclaringType.FormatCSharp(),
                    this.MethodReflectionInfo.Name
                );
        }

        private string FormatThisValue(ParamValueOutputOptions valueOutputOptions)
        {
            if(this.IsStaticMethod || valueOutputOptions == ParamValueOutputOptions.NoValue)
                return string.Empty;

            object val = this.augmentedInstance;
            Type type = val == null ? null : val.GetType();
            string thisValueStr = InterceptedMethodParamMetadata.FormatParamValue(type, val, valueOutputOptions == ParamValueOutputOptions.SlowUIValue);

            return string.Format("[this = {0}] ", thisValueStr);
        }

        private string FormatMethodParameters(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            return string.Join(", ", this.Params.Select(pinfo => pinfo.ToString(valueOutputOptions)));
        }
    }
}
