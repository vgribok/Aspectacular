#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

namespace Aspectacular
{
    /// <summary>
    ///     If implemented by DbContext entities, makes Attaching and Deleting entities easier (eliminates key finder
    ///     delegate).
    /// </summary>
    public interface IDbEntityKey
    {
        /// <summary>
        ///     Returns entity field (or collection of fields) that make up entity's unique key.
        /// </summary>
        /// <returns></returns>
        object DbContextEntityKey { get; }
    }
}