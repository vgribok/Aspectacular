#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Diagnostics;

namespace Aspectacular
{
    /// <summary>
    ///     Apply this attribute to parameters and return values
    ///     containing sensitive information, like password, so that
    ///     aspects act accordingly and not, say, log sensitive values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public class SecretParamValueAttribute : Attribute
    {
    }

    /// <summary>
    ///     Cache-ability helper: Use it to mark classes and method that
    ///     return same data when same method is called
    ///     using different instances at the same time.
    /// </summary>
    /// <remarks>
    ///     For example, if you have instance method that return data from the database,
    ///     you may want to mark it with this attribute, because when same method is called
    ///     on different instances, the returned result will be the same (instance invariant).
    ///     However, if you call DateTime.AddYear(1) on two different instances, the result
    ///     would be different, depending what date-time each instance represents.
    ///     This attribute can be used for default opt-in by applying [InstanceInvariant(true)] to the whole class,
    ///     and optional opt-out - when methods marked as [InstanceInvariant(false)].
    ///     Also, explicit opt-in can be implemented if [InstanceInvariant(true)] is applied to each individual method.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
    public class InvariantReturnAttribute : Attribute
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
        ///     Value not to be shown for parameter
        /// </summary>
        NoValue,

        /// <summary>
        ///     Value to be presented to the user.
        ///     Use sparingly as first evaluation of every value is very slow (Expression.Compile() + Reflection.Invoke()-slow).
        /// </summary>
        // ReSharper disable once InconsistentNaming
        SlowUIValue,

        /// <summary>
        ///     Value to be used internally for method result caching.
        ///     Use sparingly as first evaluation of every value is very slow (Reflection-slow).
        /// </summary>
        SlowInternalValue,
    }

    /// <summary>
    ///     Class substituting secret parameter values.
    /// </summary>
    internal class SecretValueHash
    {
        internal readonly string valHash;

        public SecretValueHash(object val)
        {
            if(val == null)
                this.valHash = null;
            else
            {
                byte[] stringBytes = Encoding.Unicode.GetBytes(val.ToString());
                long crc = ComputeCRC(stringBytes);
                this.valHash = string.Format("{0:X}-{1:X}", val.GetHashCode(), crc);
            }
        }

        // ReSharper disable once InconsistentNaming
        public static long ComputeCRC(byte[] val)
        {
            long q;
            byte c;
            long crc = 0;

            // ReSharper disable once ForCanBeConvertedToForeach
            for(int i = 0; i < val.Length; i++)
            {
                c = val[i];
                q = (crc ^ c) & 0x0f;
                crc = (crc >> 4) ^ (q*0x1081);
                q = (crc ^ (c >> 4)) & 0xf;
                crc = (crc >> 4) ^ (q*0x1081);
            }
            return crc;
        }

        public override string ToString()
        {
            return this.valHash;
        }

        public override int GetHashCode()
        {
            return this.valHash.GetHashCode();
        }
    }

    public class InterceptedMethodParamMetadata
    {
        private readonly Lazy<object> slowEvaluatingValueLoader;

        /// <summary>
        ///     Raw parameter metadata. Try use higher-level members of this class instead.
        /// </summary>
        public readonly ParameterInfo ParamReflection;

        public readonly bool CannotBeEvaluated = false;

        private readonly Expression expression;
        private readonly Lazy<ParamDirectionEnum> paramDirection;

        public InterceptedMethodParamMetadata(ParameterInfo paramReflection, Expression paramExpression, object augmentedObject, bool cannotBeEvaluated)
        {
            this.ParamReflection = paramReflection;
            this.Name = this.ParamReflection.Name;
            this.Type = this.ParamReflection.ParameterType;
            this.expression = MassageUnaryExpression(paramExpression);
            this.CannotBeEvaluated = cannotBeEvaluated;

            this.slowEvaluatingValueLoader = new Lazy<object>(() => SlowEvaluateFunctionParameter(augmentedObject));
            this.paramDirection = new Lazy<ParamDirectionEnum>(() => this.ParamReflection.GetDirection());
        }

        private object SlowEvaluateFunctionParameter(object instanceObject)
        {
            if(this.CannotBeEvaluated)
                return null;

            object val = VerySlowlyCompileAndInvoke(instanceObject, this.expression);

            if (this.ValueIsSecret)
                val = new SecretValueHash(val);

            return val;
        }

        /// <summary>
        ///     Function parameter name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Function parameter type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        ///     Parameter value retriever via slow evaluation.
        ///     Warning 1: getting this value is slow first time (for each parameter)! It's
        ///     Expression.Compile()-and-Reflection.Invoke()-slow until value is cached.
        ///     Warning 2: getting this value evaluates an input expression until value is cached. To avoid
        ///     unexpected behavior, parameter values should be invariant (having same outcome) no matter how many times parameters
        ///     is evaluated.
        /// </summary>
        /// <remarks>
        ///     If your call looks like:
        ///     <code>
        /// obj.GetProxy(aspects).Invoke(inst => inst.Foo("hello!", CreateNewUserAndGetID("John Doe")));
        /// </code>
        ///     please note that <code>CreateNewUserAndGetID("John Doe")</code> function will be called
        ///     not only when Foo() is called to pass the parameter, but also when this property value is retrieved.
        ///     Therefore, intercepted inputs should be invariant. So instead of calling
        ///     <code>inst.Foo("hello!", CreateNewUserAndGetID("John Doe"))</code>, store value returned by
        ///     <code>CreateNewUserAndGetID("John Doe")</code> and pass stored value as a parameter.
        /// </remarks>
        public object SlowEvaluatingValueLoader
        {
            get { return this.slowEvaluatingValueLoader.Value; }
        }

        public ParamDirectionEnum Direction
        {
            get { return this.paramDirection.Value; }
        }

        #region Attribute access members

        public T GetCustomAttribute<T>(bool inherit = false) where T : Attribute
        {
            return this.ParamReflection.GetCustomAttribute<T>(inherit);
        }

        public IEnumerable<T> GetCustomAttributes<T>(bool inherit = false) where T : Attribute
        {
            return this.ParamReflection.GetCustomAttributes<T>(inherit);
        }

        public bool HasAttribute<T>() where T : Attribute
        {
            return this.GetCustomAttributes<T>(false).Any();
        }

        #endregion Attribute access members

        public string FormatSlowEvaluatingValue(bool trueUi_FalseInternal)
        {
            object val = this.SlowEvaluatingValueLoader;

            Type type = val == null ? this.Type : val.GetType();

            //if(val is System.Collections.IEnumerable)
            //    throw new Exception("IEnumerable parameters are nor supported for caching because they may have different data at different times for the same instance.");

            return FormatParamValue(type, val, trueUi_FalseInternal);
        }

        public bool ValueIsSecret
        {
            get { return this.HasAttribute<SecretParamValueAttribute>(); }
        }

        public override string ToString()
        {
            return this.ToString(ParamValueOutputOptions.NoValue);
        }

        public string ToString(ParamValueOutputOptions options)
        {
            string paramStrValue = null;

            if(options != ParamValueOutputOptions.NoValue)
                paramStrValue = string.Format(" = {0}", this.FormatSlowEvaluatingValue(options == ParamValueOutputOptions.SlowUIValue));

            string paramString = string.Format("{0} {1}{2}", this.ParamReflection.FormatCSharpType(), this.Name, paramStrValue);
            return paramString;
        }

        #region Utility Methods

        /// <summary>
        ///     Be sure to use this function only if all alternative are exhausted.
        ///     It's double-slow: it compiles expression tree and make reflection-based dynamic call.
        /// </summary>
        /// <param name="augmentedObject"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object VerySlowlyCompileAndInvoke(object augmentedObject, Expression expression)
        {
            if(expression == null)
                return null;

            object slowObj;
            object fastObj = null;
            bool hasFastObj = false;

            if(expression.NodeType == ExpressionType.Constant)
            {
                ConstantExpression constExp = (ConstantExpression)expression;
                fastObj = constExp.Value;
                hasFastObj = true;
            }else
            if(expression.NodeType == ExpressionType.Parameter)
            {
                ParameterExpression pex = (ParameterExpression)expression;
                if(augmentedObject != null && pex.Type == augmentedObject.GetType())
                {
                    fastObj = augmentedObject;
                    hasFastObj = true;
                }
            }
            else
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memExpression = (MemberExpression)expression;
                if (memExpression.Expression != null && memExpression.Expression.NodeType == ExpressionType.Constant)
                {
                    ConstantExpression constExp = (ConstantExpression)memExpression.Expression;
                    object instance = constExp.Value;

                    FieldInfo fi = memExpression.Member as FieldInfo;
                    if (fi != null)
                    {
                        fastObj = fi.GetValue(instance);
                        hasFastObj = true;
                    }
                    else if (memExpression.Member is PropertyInfo)
                    {
                        PropertyInfo pi = (PropertyInfo)memExpression.Member;
                        fastObj = pi.GetValue(instance);
                        hasFastObj = true;
                    }

#if DEBUG
                    if(hasFastObj)
                    {
                        slowObj = expression.EvaluateExpressionVerySlow();
                        Debug.Assert(fastObj == slowObj || (fastObj == null && slowObj == null) || (fastObj != null && slowObj != null && fastObj.Equals(slowObj)));
                    }
#endif
                }
            }

            if(hasFastObj)
                return fastObj;

            slowObj = expression.EvaluateExpressionVerySlow();
            return slowObj;
        }

        /// <summary>
        ///     Formats parameter/this/return value as string,
        /// </summary>
        /// <param name="type"></param>
        /// <param name="val">For secret values please pass "new SecretValueHash(someValue)"</param>
        /// <param name="trueUi_FalseInternal"></param>
        /// <returns></returns>
        public static string FormatParamValue(Type type, object val, bool trueUi_FalseInternal)
        {
            if(type == typeof(void))
                return string.Empty;

            if(val == null)
                return "[null]";

            if(trueUi_FalseInternal && val is SecretValueHash)
                return "[secret]";

            if(val is string)
                return string.Format("\"{0}\"", val);

            if(val is char)
                return string.Format("'{0}'", val);

            string strVal = val.ToString();
            if(strVal == type.ToString())
                return trueUi_FalseInternal ? "[no string value]" : string.Format("HASH:{0:X}", val.GetHashCode());

            return type.IsSimpleCSharpType() ? strVal : string.Format("[{0}]", strVal);
        }

        public static Expression MassageUnaryExpression(Expression paramExpression)
        {
            if(paramExpression is UnaryExpression)
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
        ///     Raw method metadata. Use this class's members instead, if possible.
        /// </summary>
        public MethodInfo MethodReflectionInfo { get; private set; }

        public IEnumerable<Attribute> MethodAttributes
        {
            get { return this.MethodReflectionInfo.GetCustomAttributes<Attribute>(); }
        }

        private readonly object methodInstance;
        public readonly List<InterceptedMethodParamMetadata> Params = new List<InterceptedMethodParamMetadata>();


        /// <summary>
        ///     Type of method's instance owner class
        /// </summary>
        public Type ClassType
        {
            get { return this.MethodReflectionInfo.DeclaringType; }
        }

        public Type MethodReturnType
        {
            get { return this.MethodReflectionInfo.ReturnType; }
        }

        internal InterceptedMethodMetadata(object methodInstance, LambdaExpression callLambdaExp, bool forceClassInstanceInvariant)
            : this(methodInstance, callLambdaExp, forceClassInstanceInvariant, checkInstanceVsStatic: true)
        {
        }

        protected InterceptedMethodMetadata(object methodInstance, LambdaExpression callLambdaExp, bool forceClassInstanceInvariant, bool checkInstanceVsStatic)
        {
            try
            {
                this.interceptedMethodExpression = (MethodCallExpression)callLambdaExp.Body;
            }
            catch(Exception ex)
            {
                string errorText = "Intercepted method expression must be a function call. \"{0}\" is invalid in this context."
                    .SmartFormat(callLambdaExp.Body);
                throw new ArgumentException(errorText, ex);
            }

            this.methodInstance = methodInstance;
            this.MethodReflectionInfo = this.interceptedMethodExpression.Method;

            if (checkInstanceVsStatic && this.methodInstance == null && !this.MethodReflectionInfo.IsStatic)
                throw new Exception("Null instance specified for instance method \"{0}\". Please use obj.GetProxy().Invoke() instead of AOP.Invoke().".SmartFormat(this.GetMethodSignature(ParamValueOutputOptions.NoValue)));

            this.forceClassInstanceInvariant = forceClassInstanceInvariant;
            this.hasOutputParams = new Lazy<bool>(() => this.Params.Any(p => p.Direction == ParamDirectionEnum.Out || p.Direction == ParamDirectionEnum.RefInOut));

            this.InitParameterMetadata();
        }

        private void InitParameterMetadata()
        {
            ParameterInfo[] paramData = this.MethodReflectionInfo.GetParameters();

            for(int i = 0; i < paramData.Length; i++)
            {
                Expression paramExpression = this.interceptedMethodExpression.Arguments[i];

                bool canEvaluate = this.CanEvaluateParamValue(i, paramData[i], paramExpression);

                var paramMetadata = new InterceptedMethodParamMetadata(paramData[i], paramExpression, this.methodInstance, !canEvaluate);
                this.Params.Add(paramMetadata);
            }
        }

        protected virtual bool CanEvaluateParamValue(int index, ParameterInfo paramInfo, Expression paramExpression)
        {
            return true;
        }

        public object SlowEvaluateThisObject()
        {
            if(this.IsStaticMethod || this.thisObjectSlowEvaluator == null)
                return null;

            return this.thisObjectSlowEvaluator.Value;
        }

        public bool IsStaticMethod
        {
            get { return (this.MethodReflectionInfo.Attributes & System.Reflection.MethodAttributes.Static) != 0; }
        }

        /// <summary>
        ///     Determines returned data cache-ability.
        ///     Returns true if this method will return same data if called at the same time
        ///     on two or more class instances (or on the same type for static methods).
        /// </summary>
        public bool IsReturnResultInvariant
        {
            get
            {
                if(this.HasOutOrRefParams)
                    // Since we can cache only single return result,
                    // methods returning multiple values via out or ref parameters 
                    // are disqualified from being cacheable.
                    return false;

                InvariantReturnAttribute invarAttribute = this.GetMethodOrClassAttribute<InvariantReturnAttribute>();
                if(invarAttribute != null)
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

        public TAttribute GetMethodAttribute<TAttribute>(bool inherit = false) where TAttribute : Attribute
        {
            TAttribute attrib = this.MethodReflectionInfo.GetCustomAttribute<TAttribute>(inherit);
            return attrib;
        }

        public IEnumerable<TAttribute> GetMethodAttributes<TAttribute>(bool inherit = false) where TAttribute : Attribute
        {
            IEnumerable<TAttribute> attribs = this.MethodReflectionInfo.GetCustomAttributes<TAttribute>(inherit);
            return attribs;
        }

        public bool HasMethodAttribute<TAttribute>(bool inherit = false) where TAttribute : Attribute
        {
            return this.GetMethodAttributes<TAttribute>(inherit).Any();
        }

        public TAttribute GetClassAttribute<TAttribute>(bool inherit = true) where TAttribute : Attribute
        {
            TAttribute attrib = this.ClassType.GetCustomAttribute<TAttribute>(inherit);
            return attrib;
        }

        public IEnumerable<TAttribute> GetClassAttributes<TAttribute>(bool inherit = true) where TAttribute : Attribute
        {
            IEnumerable<TAttribute> attribs = this.ClassType.GetCustomAttributes<TAttribute>(inherit);
            return attribs;
        }

        public bool HasClassAttribute<TAttribute>(bool inherit = true) where TAttribute : Attribute
        {
            return this.GetClassAttributes<TAttribute>(inherit).Any();
        }

        /// <summary>
        ///     Get attribute from the method, and if not there, tries to get it from the class.
        ///     Uses default inheritance rule: "no" for method, "yes" for class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public TAttribute GetMethodOrClassAttribute<TAttribute>() where TAttribute : Attribute
        {
            TAttribute attrib = this.GetMethodAttribute<TAttribute>() ?? this.GetClassAttribute<TAttribute>();

            return attrib;
        }

        /// <summary>
        ///     Get attributes from the method, and if not there, tries to get it from the class.
        ///     Uses default inheritance rule: "no" for method, "yes" for class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public IEnumerable<TAttribute> GetMethodOrClassAttributes<TAttribute>() where TAttribute : Attribute
        {
            IEnumerable<TAttribute> attribs = this.GetMethodAttributes<TAttribute>();
// ReSharper disable PossibleMultipleEnumeration
            if(attribs.IsNullOrEmpty())
                attribs = this.GetClassAttributes<TAttribute>();

            return attribs;
// ReSharper restore PossibleMultipleEnumeration
        }

        /// <summary>
        ///     Returns true if given attribute is applied either to the method, or to its owner class.
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <returns></returns>
        public bool HasMethodOrClassAttribute<TAttribute>() where TAttribute : Attribute
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
            if(this.IsStaticMethod || valueOutputOptions == ParamValueOutputOptions.NoValue || this.IsReturnResultInvariant)
                return string.Empty;

            object val = this.methodInstance;
            Type type = val == null ? null : val.GetType();

            string thisValueStr = InterceptedMethodParamMetadata.FormatParamValue(type, val, valueOutputOptions == ParamValueOutputOptions.SlowUIValue);

            return string.Format("[this = {0}] ", thisValueStr);
        }

        private string FormatMethodParameters(ParamValueOutputOptions valueOutputOptions = ParamValueOutputOptions.NoValue)
        {
            return string.Join(", ", this.Params.Select(pinfo => pinfo.ToString(valueOutputOptions)));
        }

        public string FormatReturnResult(object returnedResult, bool trueUi_FalseInternal)
        {
            if(this.IsReturnValueSecret)
                returnedResult = new SecretValueHash(returnedResult);

            Type retType = returnedResult == null ? this.MethodReturnType : returnedResult.GetType();

            string returnValueStr = InterceptedMethodParamMetadata.FormatParamValue(retType, returnedResult, trueUi_FalseInternal);
            return returnValueStr;
        }
    }

    public class PostProcessingMethodMetadata : InterceptedMethodMetadata
    {
        public PostProcessingMethodMetadata(object methodInstance, LambdaExpression callLambdaExp, bool forceClassInstanceInvariant)
            : base(methodInstance, callLambdaExp, forceClassInstanceInvariant, checkInstanceVsStatic: false)
        {
            
        }

        protected override bool CanEvaluateParamValue(int index, ParameterInfo paramInfo, Expression paramExpression)
        {
            // First parameter is not-yet-available IQueryable<T> or IEnumerable<T> that will be later returned by main intercepted method.
            // But at this point - when intercepted method has not been run yet.
            // I know this is flimsy, but I don't have any better ideas as to how to improve it.
            return index != 0; 
        }
    }
}