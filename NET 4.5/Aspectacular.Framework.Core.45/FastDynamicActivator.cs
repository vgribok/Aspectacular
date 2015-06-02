#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Aspectacular
{
    /// <summary>
    ///     Fast dynamic object instantiator.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate object FastObjectActivator(params object[] args);

    /// <summary>
    ///     A much faster alternative to slow Activator.CreateInstance().
    /// </summary>
    public static class FastDynamicActivator
    {
        private static readonly ConcurrentDictionary<ConstructorInfo, FastObjectActivator> cachedActivators = new ConcurrentDictionary<ConstructorInfo, FastObjectActivator>();

        /// <summary>
        ///     Dynamic object activator that runs much faster than Reflection-based Activator.CreateInstance().
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="constructorArgTypes"></param>
        /// <returns></returns>
        public static FastObjectActivator GetFastActivator(this Type classType, params Type[] constructorArgTypes)
        {
            ConstructorInfo ctorInfo = FindConstructorByParams(classType, constructorArgTypes);
            if(ctorInfo == null)
                return null;

            return GetActivatorForConstructor(ctorInfo);
        }

        /// <summary>
        ///     Returns delegate that creates an instance of a given type with the packaged set of constructor arguments passed as
        ///     constructorArgs parameters.
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="constructorArgs"></param>
        /// <returns>Delegate with parameters.</returns>
        public static Func<object> GetFastActivatorWithEmbeddedArgs(this Type classType, params object[] constructorArgs)
        {
            FastObjectActivator rawActivator = GetFastActivator(classType, constructorArgs.ArgsToArgTypes());
            if(rawActivator == null)
                return null;

            Func<object> activator = () => rawActivator(constructorArgs);
            return activator;
        }

        /// <summary>
        ///     Returns delegate of a constructor for a given type, which runs much faster than Reflection-based
        ///     Activator.CreateInstance().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorArgTypes"></param>
        /// <returns></returns>
        public static Func<object[], T> GetFastActivator<T>(params Type[] constructorArgTypes)
        {
            FastObjectActivator rawActivator = typeof(T).GetFastActivator(constructorArgTypes);
            if(rawActivator == null)
                return null;

            Func<object[], T> activator = parms => (T)rawActivator(typeof(T), parms);
            return activator;
        }

        /// <summary>
        ///     Returns delegate that creates an instance of a given type with the packaged set of constructor arguments passed as
        ///     constructorArgs parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorArgs"></param>
        /// <returns></returns>
        public static Func<T> GetFastActivatorWithEmbeddedArgs<T>(params object[] constructorArgs)
        {
            FastObjectActivator rawActivator = GetFastActivator(typeof(T), constructorArgs.ArgsToArgTypes());
            if(rawActivator == null)
                return null;

            Func<T> activator = () => (T)rawActivator(constructorArgs);
            return activator;
        }

        public static FastObjectActivator GetActivatorForConstructor(this ConstructorInfo ctorInfo)
        {
            FastObjectActivator activator;
            if(!cachedActivators.TryGetValue(ctorInfo, out activator))
            {
                activator = CompileActivator(ctorInfo);
                cachedActivators[ctorInfo] = activator;
            }

            return activator;
        }

        /// <summary>
        ///     Returns collection of types for a collection of objects.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Type[] ArgsToArgTypes(this IEnumerable<object> args)
        {
            return args.Select(arg => arg == null ? typeof(object) : arg.GetType()).ToArray();
        }

        private static ConstructorInfo FindConstructorByParams(Type classType, Type[] constructorArgTypes)
        {
            if(classType == null)
                throw new ArgumentNullException("classType");

            foreach(ConstructorInfo ctorInfo in classType.GetConstructors())
            {
                if(!ctorInfo.IsPublic)
                    continue;

                ParameterInfo[] parms = ctorInfo.GetParameters();
                if(parms.Length != constructorArgTypes.Length)
                    continue;

                if(constructorArgTypes.Length == 0)
                    return ctorInfo;

                bool sameType = true;

                for(int i = 0; sameType && i < constructorArgTypes.Length; i++)
                {
                    Type paramType = parms[i].ParameterType;

                    if(constructorArgTypes[i] == null)
                        sameType = paramType.IsClass;
                    else
                    {
                        Type argType = constructorArgTypes[i];
                        sameType = argType == paramType || argType.IsSubclassOf(paramType);
                    }
                }

                if(sameType)
                    return ctorInfo;
            }

            return null;
        }

        /// <summary>
        ///     Improved version of the activator compiler glanced at
        ///     http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
        /// </summary>
        /// <param name="ctorInfo"></param>
        /// <returns></returns>
        private static FastObjectActivator CompileActivator(this ConstructorInfo ctorInfo)
        {
            if(ctorInfo == null)
                throw new ArgumentNullException("ctorInfo");

            ParameterInfo[] paramsInfo = ctorInfo.GetParameters();

            //create a single param of type object[]
            ParameterExpression constructorArgsExp = Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp = new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for(int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp = Expression.ArrayIndex(constructorArgsExp, index);
                UnaryExpression paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctorInfo, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            Expression<FastObjectActivator> lambda = Expression.Lambda<FastObjectActivator>(newExp, constructorArgsExp);
            FastObjectActivator constructorDelegate = lambda.Compile();

            return constructorDelegate;
        }
    }
}