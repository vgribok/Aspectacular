using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Data.EntityFramework
{
    /// <summary>
    /// If implemented by DbContext entities, makes Attaching and Deleting entities easier (eliminates key finder delegate).
    /// </summary>
    public interface IDbEntityKey
    {
        /// <summary>
        /// Returns entity field (or collection of fields) that make up entity's unique key.
        /// </summary>
        /// <returns></returns>
        object DbContextEntityKey { get; }
    }
}
