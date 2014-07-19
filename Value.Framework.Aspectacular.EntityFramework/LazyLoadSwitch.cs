#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Data.Entity;
using System.Data.Objects;

namespace Aspectacular
{
    /// <summary>
    ///     Implementation of disposable pattern for
    ///     temporarily turning on EF context class's lazy loading.
    /// </summary>
    /// <typeparam name="TDb"></typeparam>
    public class TempLazyLoadOn<TDb> : IDisposable
        where TDb : class
    {
        private TDb db;
        private Action<TDb, bool> lazyLoadSwitch;

        /// <summary>
        /// </summary>
        /// <param name="tdb">DbContext or ObjectContext</param>
        /// <param name="lazyLoadCheck">Returns current value of the EF context's lazy load flag.</param>
        /// <param name="lazyLoadSwitch">Sets EF context's lazy load flag</param>
        public TempLazyLoadOn(TDb tdb, Func<TDb, bool> lazyLoadCheck, Action<TDb, bool> lazyLoadSwitch)
        {
            if(lazyLoadCheck(tdb) == false)
            {
                this.db = tdb;
                this.lazyLoadSwitch = lazyLoadSwitch;
                this.lazyLoadSwitch(tdb, true);
            }
        }

        public void Dispose()
        {
            if(this.db != null)
            {
                this.lazyLoadSwitch(this.db, false);

                this.db = null;
                this.lazyLoadSwitch = null;
            }
        }
    }

    /// <summary>
    ///     Serialization of EF entities is possible when their parent DB/Object context
    ///     has lazy load turned off. So to enable lazy loading temporarily, lazy load
    ///     can be turned on temporarily, using Disposable pattern.
    /// </summary>
    public static class LazyLoadOnEx
    {
        public static TempLazyLoadOn<DbContext> TurnLazyLoadOn(this DbContext dbc)
        {
            return new TempLazyLoadOn<DbContext>(dbc, dbctx => dbctx.Configuration.LazyLoadingEnabled, (dbctx, flag) => dbctx.Configuration.LazyLoadingEnabled = flag);
        }

        public static TempLazyLoadOn<ObjectContext> TurnLazyLoadOn(this ObjectContext dbc)
        {
            return new TempLazyLoadOn<ObjectContext>(dbc, dbctx => dbctx.ContextOptions.LazyLoadingEnabled, (dbctx, flag) => dbctx.ContextOptions.LazyLoadingEnabled = flag);
        }
    }
}