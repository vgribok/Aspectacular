using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Core
{
    /// <summary>
    /// Fast dynamic object instantiator.
    /// </summary>
    /// <param name="classType"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate object FastObjectActivator(Type classType, params object[] args);

    public delegate T FastActivator<T>(params object[] args);

    public static class FastDynamicActivator
    {
        private static readonly ConcurrentDictionary<ConstructorInfo, FastObjectActivator> cachedActivators = new ConcurrentDictionary<ConstructorInfo, FastObjectActivator>();

        /// <summary>
        /// Dynamic object activator that runs much faster than Reflection-based Activator.CreateInstance().
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static FastObjectActivator GetFastActivator(this Type classType, params object[] args)
        {
            ConstructorInfo ctorInfo = FindConstructorByParams(classType, args);
            if(ctorInfo == null)
                return null;

            return GetActivator(ctorInfo);
        }

        /// <summary>
        /// Dynamic object activator that runs much faster than Reflection-based Activator.CreateInstance().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        public static FastActivator<T> GetFastActivator<T>(params object[] args)
        {
            FastObjectActivator activator = typeof(T).GetFastActivator(args);
            return new FastActivator<T>(parms => (T)activator(typeof(T), parms));
        }

        private static ConstructorInfo FindConstructorByParams(Type classType, params object[] args)
        {
            if (classType == null)
                throw new ArgumentNullException("classType");

            List<ConstructorInfo> ctors = new List<ConstructorInfo>();

            foreach (ConstructorInfo ctorInfo in classType.GetConstructors())
            {
                if(!ctorInfo.IsPublic)
                    continue;

                ParameterInfo[] parms = ctorInfo.GetParameters();
                if (parms.Length != args.Length)
                    continue;

                if(args.Length == 0)
                    return ctorInfo;

                bool sameType = true;

                for (int i = 0; sameType && i < args.Length; i++)
                {
                    Type paramType = parms[i].ParameterType;

                    if (args[i] == null)
                        sameType = paramType.IsClass;
                    else
                    {
                        Type argType = args[i].GetType();
                        sameType = argType == paramType || argType.IsSubclassOf(paramType);
                    }
                }

                if (sameType)
                    return ctorInfo;
            }

            return null;
        }

        public static FastObjectActivator GetActivator(this ConstructorInfo ctorInfo)
        {
            FastObjectActivator activator;
            if (!cachedActivators.TryGetValue(ctorInfo, out activator))
            {
                activator = CompileActivator(ctorInfo);
                cachedActivators[ctorInfo] = activator;
            }

            return activator;
        }

        /// <summary>
        /// Improved version of the activator compiler glanced at
        /// http://rogeralsing.com/2008/02/28/linq-expressions-creating-objects/
        /// </summary>
        /// <param name="ctorInfo"></param>
        /// <returns></returns>
        private static FastObjectActivator CompileActivator(this ConstructorInfo ctorInfo)
        {
            if (ctorInfo == null)
                throw new ArgumentNullException("ctorInfo");

            Type classType = ctorInfo.DeclaringType;
            ParameterInfo[] paramsInfo = ctorInfo.GetParameters();

            ParameterExpression typeArgExp = Expression.Parameter(typeof(Type), "classType");
            //create a single param of type object[]
            ParameterExpression constructorArgsExp = Expression.Parameter(typeof(object[]), "args");

            UnaryExpression[] argsExp = new UnaryExpression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
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
            Expression<FastObjectActivator> lambda = Expression.Lambda<FastObjectActivator>(newExp, typeArgExp, constructorArgsExp);
            FastObjectActivator constructorDelegate = lambda.Compile();

            return constructorDelegate;
        }
    }
}
