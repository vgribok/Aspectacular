#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System.Collections.Generic;

namespace Aspectacular
{
    /// <summary>
    ///     Alloc/invoke/dispose convenience class for DalManager subclasses.
    /// </summary>
    /// <typeparam name="TMultiStoreMgr"></typeparam>
    public class DalSingleCallProxy<TMultiStoreMgr> : DbEngineProxy<TMultiStoreMgr>
        where TMultiStoreMgr : DalManager, new()
    {
        public DalSingleCallProxy(IEnumerable<Aspect> aspects)
            : base(aspects)
        {
        }

        /// <summary>
        ///     A pass-through Proxy constructor that creates Proxy which won't clean up instance after method invocation.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="aspects"></param>
        public DalSingleCallProxy(TMultiStoreMgr instance, IEnumerable<Aspect> aspects)
            : base(instance, aspects)
        {
        }

        public override int CommitChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }
    }

// ReSharper disable once InconsistentNaming
    public static partial class AOP
    {
        /// <summary>
        ///     Returns AOP proxy for a multi-data store DAL object.
        /// </summary>
        /// <typeparam name="TMultiStoreMgr"></typeparam>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DalSingleCallProxy<TMultiStoreMgr> GetDalProxy<TMultiStoreMgr>(IEnumerable<Aspect> aspects = null)
            where TMultiStoreMgr : DalManager, new()
        {
            var proxy = new DalSingleCallProxy<TMultiStoreMgr>(aspects);
            return proxy;
        }
    }

    public static partial class AopExsts
    {
        /// <summary>
        ///     Returns InstanceProxy[TMultiStoreMgr] for DalManager instance that already exist.
        ///     Returned proxy won't call DalManager.Dispose() after method invocation.
        ///     Supports ExecuteCommand() method.
        /// </summary>
        /// <typeparam name="TMultiStoreMgr"></typeparam>
        /// <param name="existingInstance"></param>
        /// <param name="aspects"></param>
        /// <returns></returns>
        public static DalSingleCallProxy<TMultiStoreMgr> GetDalProxy<TMultiStoreMgr>(this TMultiStoreMgr existingInstance, IEnumerable<Aspect> aspects = null)
            where TMultiStoreMgr : DalManager, new()
        {
            var proxy = new DalSingleCallProxy<TMultiStoreMgr>(existingInstance, aspects);
            return proxy;
        }
    }
}