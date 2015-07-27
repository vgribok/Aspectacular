#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;

namespace Aspectacular
{
    public class RunDisposeProxy<TDispClass> : InstanceProxy<TDispClass>
        where TDispClass : class, IDisposable
    {
        protected internal RunDisposeProxy(TDispClass instance, IEnumerable<Aspect> aspects, bool autoDispose = false)
            : base(() => instance, autoDispose ? Cleanup : (Action<TDispClass>)null, aspects)
        {
        }

        public RunDisposeProxy(Func<TDispClass> factory, IEnumerable<Aspect> aspects)
            : base(factory, Cleanup, aspects)
        {
        }

        private static void Cleanup(TDispClass instance)
        {
            if(instance != null)
                instance.Dispose();
        }
    }

    public class AllocateRunDisposeProxy<TDispClass> : RunDisposeProxy<TDispClass>
        where TDispClass : class, IDisposable, new()
    {
        /// <summary>
        ///     A pass-through constructor that creates proxy which does neither instantiate nor cleans up the instance after it's
        ///     used.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        /// <param name="autoDispose">If true, Dispose() will be called after the end of the intercepted call.</param>
        public AllocateRunDisposeProxy(TDispClass instance, IEnumerable<Aspect> aspects, bool autoDispose = false)
            : base(instance, aspects, autoDispose)
        {
        }

        /// <summary>
        ///     Creates proxy that instantiates IDisposable class
        ///     and after method invocation calls class's Dispose().
        /// </summary>
        /// <param name="aspects"></param>
        public AllocateRunDisposeProxy(IEnumerable<Aspect> aspects)
            : base(Instantiate, aspects)
        {
        }

        private static TDispClass Instantiate()
        {
            TDispClass instance = new TDispClass();
            return instance;
        }
    }

// ReSharper disable once InconsistentNaming
    public static partial class AOP
    {
        /// <summary>
        ///     Returns AOP proxy for TDispClass class derived from IDisposable.
        ///     The proxy will instantiate the TDispClass object before making the intercepted method call,
        ///     and dispose of the instance after the intercepted method call.
        /// </summary>
        /// <typeparam name="TDispClass"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static AllocateRunDisposeProxy<TDispClass> GetProxy<TDispClass>(IEnumerable<Aspect> aspects = null)
            where TDispClass : class, IDisposable, new()
        {
            var proxy = new AllocateRunDisposeProxy<TDispClass>(aspects);
            return proxy;
        }

        /// <summary>
        /// Returns AOP-enabled service interface previously registered using SvcLocator.Register().
        /// </summary>
        /// <typeparam name="TDispInterface">An interface subclassing IDisposable.</typeparam>
        /// <param name="aspects"></param>
        /// <returns>AOP proxy representing service interface.</returns>
        public static RunDisposeProxy<TDispInterface> GetDispService<TDispInterface>(IEnumerable<Aspect> aspects = null)
            where TDispInterface : class, IDisposable
        {
            RunDisposeProxy<TDispInterface> proxy = new RunDisposeProxy<TDispInterface>(SvcLocator.Get<TDispInterface>(), aspects);
            return proxy;
        }
    }
}
