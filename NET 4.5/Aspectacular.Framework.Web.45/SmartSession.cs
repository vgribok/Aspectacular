#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Web;

namespace Aspectacular
{
    public static class SessionExtensions
    {
        /// <summary>
        ///     Factory maintaining strongly-typed session state instance.
        ///     Create a [Serializable] class with all your session state properties, and
        ///     then simply use this method to return new or existing instance of the session state.
        ///     Also, create static Current property in the class, returning HttpContext.Current.SmartSession[T](); for ease of
        ///     use.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static T SmartSession<T>(this HttpContext ctx) where T : class, new()
        {
            if(!typeof(T).IsSerializable)
                throw new Exception("Type \"{0}\" must be serializable in order to serve as session state storage.".SmartFormat(typeof(T).FormatCSharp()));

            if(ctx == null)
                ctx = HttpContext.Current;

            if(ctx == null || ctx.Session == null)
                return null;

            const string smartSessionKey = "VH_Smart_Session_Object";
            T session = ctx.Session[smartSessionKey] as T;
            if(session == null)
            {
                session = new T();
                ctx.Session[smartSessionKey] = session;
            }

            return session;
        }
    }
}