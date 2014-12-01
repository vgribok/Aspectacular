using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{

    /// <summary>
    /// Structure defining common query modifications,
    /// like paging, sorting and filtering.
    /// </summary>
    public class QueryModifiers<TEntity>
    {
        /// <summary>
        /// Defines query paging modifier. 
        /// Modified query will bring only a subset of data 
        /// not exceeding in size the number of records specified by PageSize property.
        /// </summary>
        public class Paging
        {
            /// <summary>
            /// Zero-based page index;
            /// </summary>
            public int PageIndex { get; set; }

            /// <summary>
            /// Data page size in the number of records.
            /// </summary>
            public int PageSize { get; set; }
        }

        /// <summary>
        /// Optional data page information.
        /// If null, no paging is performed.
        /// </summary>
        public Paging PageInfo { get; set; }

        /// <summary>
        /// Modifies a query
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query">Modifications to be applied to the query</param>
        /// <returns></returns>
        public IQueryable<TEntity> AugmentQuery(IQueryable<TEntity> query)
        {
            if (query == null)
                return null;

            throw new NotImplementedException("AugmentQuery");
        }
    }

    public static class QueryModifiersExtensions
    {
        /// <summary>
        /// Modifies a query by applying paging, sorting and filtering.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query">Query to modify.</param>
        /// <param name="optionalModifiers">Modifications to be applied to the query</param>
        /// <returns></returns>
        public static IQueryable<TEntity> AugmentQuery<TEntity>(this IQueryable<TEntity> query, QueryModifiers<TEntity> optionalModifiers = null)
        {
            return optionalModifiers == null ? query : optionalModifiers.AugmentQuery(query);
        }
    }
}
