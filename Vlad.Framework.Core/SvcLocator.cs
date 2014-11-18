#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;

namespace Aspectacular
{
    /// <summary>
    ///     Realizes simplest possible "IoC" container, limited to resolving interface based on interface-class type map.
    /// </summary>
    public static class SvcLocator
    {
        private static readonly Dictionary<Type, Func<object>> interfaceImplementationMap = new Dictionary<Type, Func<object>>();

        /// <summary>
        ///     Returns new instance of a TInterface implementation.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <returns></returns>
        public static TInterface Get<TInterface>() where TInterface : class
        {
            Func<object> instantiator;

            Type tInterface = typeof(TInterface);
            if(!tInterface.IsInterface)
                throw new InvalidOperationException(string.Format("\"{0}\" must be an interface.", tInterface));

            lock(interfaceImplementationMap)
            {
                interfaceImplementationMap.TryGetValue(tInterface, out instantiator);
            }

            if(instantiator == null)
                throw new InvalidOperationException("Use Register() before calling Get().");

            TInterface instance = (TInterface)instantiator();
            return instance;
        }

        /// <summary>
        ///     Registers interface-implementation class mapping.
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <typeparam name="TImplementation">Class type implementing interface.</typeparam>
        public static void Register<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            Register<TInterface>(() => new TImplementation());
        }

        /// <summary>
        ///     Registers interface-implementation mapping.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="resolver"></param>
        public static void Register<TInterface>(Func<TInterface> resolver)
            where TInterface : class
        {
            lock (interfaceImplementationMap)
            {
                interfaceImplementationMap[typeof(TInterface)] = resolver;
            }
        }
    }
}