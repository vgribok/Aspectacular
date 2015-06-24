#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Linq;

namespace Aspectacular
{
    public enum WhenRequiredAspectIsMissing
    {
        /// <summary>
        ///     Tells to check existing aspect collection only, and tail if aspect is not found there.
        /// </summary>
        DontInstantiate = 0,

        /// <summary>
        ///     Tells to instantiate aspect if it's not found in the aspect collection,
        ///     and append it to the end of the aspect collection.
        /// </summary>
        InstantiateAndAppend,

        /// <summary>
        ///     Tells to instantiate aspect if it's not found in the aspect collection,
        ///     and insert it to at the beginning of the aspect collection.
        /// </summary>
        InstantiateAndAddFirst,
    }

    /// <summary>
    /// Use on methods or classes that require an aspect.
    /// Proxy will throw an exception if the method decorated with this attribute is not called with the specified aspect.
    /// This attribute has no effect on calls made without AOP.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequiredAspectAttribute : Attribute
    {
        protected readonly Func<Aspect> activator = null;

        /// <summary>
        ///     Use to ensure certain aspect is used when method is intercepted.
        /// </summary>
        /// <param name="aspectType"></param>
        /// <param name="missingAspectOption"></param>
        /// <param name="constructorArgs"></param>
        public RequiredAspectAttribute(Type aspectType, WhenRequiredAspectIsMissing missingAspectOption, params object[] constructorArgs)
        {
            if(aspectType == null)
                throw new ArgumentNullException("aspectType");

            if(!aspectType.IsSubclassOf(typeof(Aspect)))
                throw new ArgumentException("aspectType must be a subclass of the Aspect class.");

            this.AspectClassType = aspectType;

            if(missingAspectOption != WhenRequiredAspectIsMissing.DontInstantiate)
            {
                // Need to instantiate missing aspect
                Func<object> rawActivator = aspectType.GetFastActivatorWithEmbeddedArgs(constructorArgs);
                if(rawActivator != null)
                {
                    this.activator = () => (Aspect)rawActivator();
                } else
                {
                    // No activator found
                    string strErrorMsg = "Unable to find {0}({1}) constructor necessary to instantiate required missing aspect {0}."
                        .SmartFormat(aspectType.FormatCSharp(),
                            string.Join(", ", constructorArgs.Select(arg => arg == null ? "[ANY]" : arg.GetType().FormatCSharp()))
                        );

                    throw new ArgumentException(strErrorMsg, "aspectType");
                }
            }

            this.InstantiateIfMissing = missingAspectOption;
        }

        public Type AspectClassType { get; protected set; }

        public WhenRequiredAspectIsMissing InstantiateIfMissing { get; protected set; }

        protected bool CanCreateAspectInstance
        {
            get { return this.activator != null; }
        }

        public Aspect InstantiateAspect()
        {
            Aspect aspect = this.activator();
            return aspect;
        }
    }
}