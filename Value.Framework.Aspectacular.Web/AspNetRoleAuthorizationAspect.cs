using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Value.Framework.Core;

namespace Value.Framework.Aspectacular.Web
{
    /// <summary>
    /// Decorate methods and classes with this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class DemandAspNetRoleAttribute : System.Attribute
    {
        protected readonly bool demandAny;
        protected readonly string[] demandedRoles;

        /// <summary>
        /// Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        /// </summary>
        /// <param name="trueDemandAny_falseDemandAll">If true, user must me a member of *at least one* role. If false, user must be a member of *all* roles.</param>
        /// <param name="demandedRoles">Collection of roles in order to authorize access to a class or method called using AOP proxy.</param>
        public DemandAspNetRoleAttribute(bool trueDemandAny_falseDemandAll, params string[] demandedRoles)
        {
            if (demandedRoles.IsNullOrEmpty())
                throw new ArgumentNullException("roles");

            this.demandAny = trueDemandAny_falseDemandAll;
            this.demandedRoles = demandedRoles;
        }

        /// <summary>
        /// Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        /// User must be a member of at least one demanded role in order to be authorized.
        /// </summary>
        /// <param name="demandedRoles"></param>
        public DemandAspNetRoleAttribute(params string[] demandedRoles)
            : this(trueDemandAny_falseDemandAll: true, demandedRoles: demandedRoles)
        {
        }

        internal bool IsCurrentUserAuthorized()
        {
            if (this.demandedRoles.Length == 1)
                return Roles.IsUserInRole(this.demandedRoles[0]);

            string[] userRoles = Roles.GetRolesForUser();

            int roleMemshipCount = userRoles.Intersect(this.demandedRoles).Count();

            if (this.demandAny)
                return roleMemshipCount > 0;
            else
                // demand all
                return roleMemshipCount == this.demandedRoles.Length;
        }
    }

    /// <summary>
    /// Enforces ASP.NET role membership authorization for methods or classes decorated with the DemandAspNetRoleAttribute.
    /// </summary>
    public class AspNetRoleAuthorizationAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            this.EnsureUserAspNetRoleAuthorization();
        }

        protected void EnsureUserAspNetRoleAuthorization()
        {
            DemandAspNetRoleAttribute aspNetRoleDem = this.Context.InterceptedCallMetaData.GetMethodOrClassAttribute<DemandAspNetRoleAttribute>();
            if (aspNetRoleDem == null)
                return;

            if (aspNetRoleDem.IsCurrentUserAuthorized())
                return;

            try
            {
                HttpContext.Current.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            }
            catch
            {
                // No biggy if status code has already be written, we can skip this.
            }

            throw new Exception(string.Format("User \"{0}\" is not authorized to call this function.", HttpContext.Current.User.Identity.Name));
        }
    }
}
