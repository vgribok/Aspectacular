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
    /// <summary>
    /// Apply this attribute to parameters and return values 
    /// containing sensitive information, like password, so that
    /// aspects act accordingly and not, say, log sensitive values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class SecretParamValueAttribute : System.Attribute
    {
    }

    /// <summary>
    /// Cache-ability helper: Use it to mark classes and method that
    /// return same data when same method is called 
    /// using different instances at the same time.
    /// </summary>
    /// <remarks>
    /// For example, if you have instance method that return data from the database,
    /// you may want to mark it with this attribute, because when same method is called
    /// on different instances, the returned result will be the same (instance invariant).
    /// However, if you call DateTime.AddYear(1) on two different instances, the result
    /// would be different, depending what date-time each instance represents.
    /// 
    /// This attribute can be used for default opt-in by applying [InstanceInvariant(true)] to the whole class, 
    /// and optional opt-out - when methods marked as [InstanceInvariant(false)].
    /// Also, explicit opt-in can be implemented if [InstanceInvariant(true)] is applied to each individual method.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class InvariantReturnAttribute : System.Attribute
    {
        public bool IsInstanceInvariant { get; private set; }

        public InvariantReturnAttribute(bool isInstanceInvariant = true)
        {
            this.IsInstanceInvariant = isInstanceInvariant;
        }
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

        /// <summary>
        /// Raw parameter metadata. Try use higher-level members of this class instead.
        /// </summary>
        public readonly ParameterInfo ParamReflection;

        private readonly Expression expression;
        private readonly Lazy<ParamDirectionEnum> paramDirection;

        public InterceptedMethodParamMetadata(ParameterInfo paramReflection, Expression paramExpression)
        {
            this.ParamReflection = paramReflection;
            this.Name = this.ParamReflection.Name;
            this.Type = this.ParamReflection.ParameterType;
            this.expression = MassageUnaryExpression(paramExpression);

            this.slowEvaluatingValueLoader = new Lazy<object>(() =>
            {
                object val = VerySlowlyCompileAndInvoke(this.expression);

                if (this.ValueIsSecret)
                    val = new SecretValueHash(val);

                return val;
            });

            this.paramDirection = new Lazy<ParamDirectionEnum>(() => this.ParamReflection.GetDirection());
        }

        /// <summary>
        /// Function parameter name.
        /// </summary>
        public string Name { get; private set;  }
        
        /// <summary>
        /// Function parameter type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Parameter value retriever via slow evaluation.
        /// 
        /// Warning 1: getting this value is slow first time (for each parameter)! It's Expression.Compile()-and-Reflection.Invoke()-slow until value is cached.
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

        public ParamDirectionEnum Direction
        {
            get { return this.paramDirection.Value; }
        }

        #region Attribute access members

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

        #endregion Attribute access members

        public string FormatSlowEvaluatingValue(bool trueUI_falseInternal)
        {
            object val = this.SlowEvaluatingValueLoader;

            return FormatParamValue(this.Type, val, trueUI_falseInternal);
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
                paramStrValue = string.Format(" = {0}", this.FormatSlowEvaluatingValue(options == ParamValueOutputOptions.SlowUIValue));

            string paramString = string.Format("{0} {1}{2}", this.ParamReflection.FormatCSharpType(), this.Name, paramStrValue);
            return paramString;
        }

        #region Utility Methods

        /// <summary>
        /// Be sure to use this function only if all alternative are exhausted. 
        /// It's double-slow: it compiles expression tree and make reflection-based dynamic call.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Formats parameter/this/return value as string, 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="val">For secret values please pass "new SecretValueHash(someValue)"</param>
        /// <param name="trueUI_falseInternal"></param>
        /// <returns></returns>
        public static string FormatParamValue(Type type, object val, bool trueUI_falseInternal)
        {
            if (type.Equals(typeof(void)))
                return string.Empty;

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

            return type.IsSimpleCSharpType() ? strVal : string.Format("[{0}]", strVal);
        }

        public static Expression MassageUnaryExpression(Expression paramExpression)
        {
            if (paramExpression is UnaryExpression)
                return MassageUnaryExpression((paramExpression as UnaryExpression).Operand);

            return paramExpression;
        }

        #endregion Utility Methods
    }

    public class InterceptedMethodMetadata
    {
        private readonly Lazy<object> thisObjectSlowEvaluator = null;
        protected MethodCallExpression interceptedMethodExpression;
        private readonly bool forceClassInstanceInvariant;
        private readonly Lazy<bool> hasOutputParams;


        /// <summary>
        /// Raw method metadata. Use this class's members instead, if possible.
        /// </summary>
        public MethodInfo MethodReflectionInfo { get; private set; }
        public IEnumerable<Attribute> MethodAttributes { get { return this.MethodReflectionInfo.GetCustomAttributes(); } }
        private readonly object augmentedInstance;
        public readonly List<InterceptedMethodParamMetadata> Params = new List<InterceptedMethodParamMetadata>();


        /// <summary>
        /// Type of method's instance owner class
        /// </summary>
        public Type ClassType
        {
            get { return this.MethodReflectionInfo.DeclaringType; }
        }

        public Type MethodReturnType
        {
            get { return this.MethodReflectionInfo.ReturnType;  }
        }

        internal InterceptedMethodMetadata(object augmentedInstance, LambdaExpression callLambdaExp, bool forceClassInstanceInvariant)
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
            this.forceClassInstanceInvariant = forceClassInstanceInvariant;
            this.hasOutputParams = new Lazy<bool>(() => this.Params.Any(p => p.Direction == ParamDirectionEnum.Out || p.Direction == ParamDirectionEnum.RefInOut));

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

        /// <summary>
        /// Determines returned data cache-ability.
        /// Returns true if this method will return same data if called at the same time
        /// on two or more class instances (or on the same type for static methods).
        /// </summary>
        public bool IsReturnResultInvariant
        {
            get
            {
                if (this.HasOutOrRefParams)
                    // Since we can cache only single return result,
                    // methods returning multiple values via out or ref parameters 
                    // are disqualified from being cacheable.
                    return false;

                InvariantReturnAttribute invarAttribute = this.GetMethodOrClassAttribute<InvariantReturnAttribute>();
                if (invarAttribute != null)
                    return invarAttribute.IsInstanceInvariant;

                return this.forceClassInstanceInvariant;
            }
        }

        public bool IsReturnValueSecret
        {
            get
            {
                SecretParamValueAttribute secretAttrib = this.MethodReflectionInfo.ReturnParameter.GetCustomAttribute<SecretParamValueAttribute>();
                return secretAttrib != null;
            }
        }

        public bool HasOutOrRefParams
        {
            get { return this.hasOutputParams.Value; }
        }

        public string GetMethodSignature(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            string signature = string.Format("{0} {1}({2})",
                this.MethodReturnType.FormatCSharp(),
                this.FormatMethodName(valueOutputOptions),
                this.FormatMethodParameters(valueOutputOptions)
                );

            return signature;
        }

        #region Attribute access members

        public TAttribute GetMethodAttribute<TAttribute>(bool inherit = false) where TAttribute : System.Attribute
        {
            TAttribute attrib = this.MethodReflectionInfo.GetCustomAttribute<TAttribute>(inherit);
            return attrib;
        }

        public IEnumerable<TAttribute> GetMethodAttributes<TAttribute>(bool inherit = false) where TAttribute : System.Attribute
        {
            IEnumerable <TAttribute> attribs = this.MethodReflectionInfo.GetCustomAttributes<TAttribute>(inherit);
            return attribs;
        }

        public bool HasMethodAttribute<TAttribute>(bool inherit = false) where TAttribute : System.Attribute
        {
            return this.GetMethodAttributes<TAttribute>(inherit).Any();
        }

        public TAttribute GetClassAttribute<TAttribute>(bool inherit = true) where TAttribute : System.Attribute
        {
            TAttribute attrib = this.ClassType.GetCustomAttribute<TAttribute>(inherit);
            return attrib;
        }

        public IEnumerable<TAttribute> GetClassAttributes<TAttribute>(bool inherit = true) where TAttribute : System.Attribute
        {
            IEnumerable<TAttribute> attribs = this.ClassType.GetCustomAttributes<TAttribute>(inherit);
            return attribs;
        }

        public bool HasClassAttribute<TAttribute>(bool inherit = true) where TAttribute : System.Attribute
        {
            return this.GetClassAttributes<TAttribute>(inherit).Any();
        }

        /// <summary>
        /// Get attribute from the method, and if not there, tries to get it from the class.
        /// Uses default inheritance rule: "no" for method, "yes" for class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public TAttribute GetMethodOrClassAttribute<TAttribute>() where TAttribute : System.Attribute
        {
            TAttribute attrib = this.GetMethodAttribute<TAttribute>();
            if (attrib == null)
                attrib = this.GetClassAttribute<TAttribute>();

            return attrib;
        }

        /// <summary>
        /// Get attributes from the method, and if not there, tries to get it from the class.
        /// Uses default inheritance rule: "no" for method, "yes" for class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public IEnumerable<TAttribute> GetMethodOrClassAttributes<TAttribute>() where TAttribute : System.Attribute
        {
            IEnumerable<TAttribute> attribs = this.GetMethodAttributes<TAttribute>();
            if (attribs.IsNullOrEmpty())
                attribs = this.GetClassAttributes<TAttribute>();

            return attribs;
        }

        /// <summary>
        /// Returns true if given attribute is applied either to the method, or to its owner class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public bool HasMethodOrClassAttribute<TAttribute>() where TAttribute : System.Attribute
        {
            return this.GetMethodOrClassAttributes<TAttribute>().Any();
        }

        #endregion Attribute access members

        private string FormatMethodName(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            return string.Format("{0}{1}.{2}",
                this.IsStaticMethod ? "static " : this.FormatThisValue(valueOutputOptions),
                    this.ClassType.FormatCSharp(),
                    this.MethodReflectionInfo.Name
                );
        }

        private string FormatThisValue(ParamValueOutputOptions valueOutputOptions)
        {
            if (this.IsStaticMethod || valueOutputOptions == ParamValueOutputOptions.NoValue || this.IsReturnResultInvariant)
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

        public string FormatReturnResult(object returnedResult, bool trueUI_falseInternal)
        {
            if (this.IsReturnValueSecret)
                returnedResult = new SecretValueHash(returnedResult);

            string returnValueStr = InterceptedMethodParamMetadata.FormatParamValue(this.MethodReturnType, returnedResult, trueUI_falseInternal);
            return returnValueStr;
        }
    }
}
