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
    public class InterceptedMethodMetadata
    {
        protected MethodCallExpression interceptedMethodExpression;
        public MethodInfo MethodReflectionInfo { get; private set; }
        public IEnumerable<Attribute> MethodAttributes { get { return this.MethodReflectionInfo.GetCustomAttributes(); } }

        public string GetMethodSignature()
        {
            string signature = string.Format("{0} {1}({2})",
                this.MethodReflectionInfo.ReturnType.FormatCSharp(),
                this.FormatMethodName(),
                this.FormatMethodParameters()
                );

            return signature;
        }

        private string FormatMethodName()
        {
            return string.Format("{0}{1}.{2}",
                (this.MethodReflectionInfo.Attributes & System.Reflection.MethodAttributes.Static) == 0 ? string.Empty : "static ",
                    this.MethodReflectionInfo.DeclaringType.Name,
                    this.MethodReflectionInfo.Name
                );
        }

        private string FormatMethodParameters()
        {
            return string.Join(", ",
                this.MethodReflectionInfo.GetParameters().Select(parmInfo => string.Format("{0} {1}", parmInfo.FormatCSharpType(), parmInfo.Name)));
        }

        public InterceptedMethodMetadata(LambdaExpression callLambdaExp)
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

            this.MethodReflectionInfo = this.interceptedMethodExpression.Method;
        }
    }
}
