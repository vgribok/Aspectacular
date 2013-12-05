using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular.EntityFramework
{
    public interface IEfCallInterceptor
    {
        /// <summary>
        /// Result of ObjectContex or DbContext .SaveChanges() call.
        /// </summary>
        int SaveChangeReturnValue { get; set; }

        /// <summary>
        /// Proxy for calling DbContext and ObjectContext SaveChanges() method.
        /// </summary>
        /// <returns></returns>
        int SaveChanges();
    }

    public static class IEfCallInterceptorExt
    {
        public static void DoSaveChanges(this IEfCallInterceptor efInterceptor)
        {
            efInterceptor.SaveChangeReturnValue = efInterceptor.SaveChanges();
        }
    }

    public class DbContextSingleCallInterceptor<TDbContext> : AllocateRunDisposeInterceptor<TDbContext>, IEfCallInterceptor
        where TDbContext : DbContext, new()
    {
        public DbContextSingleCallInterceptor(params Aspect[] aspects)
            : base(aspects)
        {
        }

        public int SaveChangeReturnValue { get; set; }

        public int SaveChanges()
        {
            return this.AugmentedClassInstance.SaveChanges();
        }

        protected override void InvokeInterceptedMethod()
        {
            base.InvokeInterceptedMethod();
            this.DoSaveChanges();
        }
    }

    public static partial class EfAOP
    {
        // TODO : Invoke that returns SaveChanges() int result.
    }
}
