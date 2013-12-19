using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Value.Framework.Aspectacular.Aspects;
using Value.Framework.Core;

namespace Value.Framework.Aspectacular.Web
{
    /// <summary>
    /// Decorate methods and classes with this attribute.
    /// Caution: Uses Roles.GetRolesForUser() internally, which has weird caching policy.
    /// </summary>
    /// <remarks>
    /// For better performance, implement your own ClaimsIdentity for ASP.NET Roles 
    /// that caches claims for longer periods of time, and then use DemandClaimsAttribute instead
    /// of DemandAspNetRoleAttribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class DemandAspNetRoleAttribute : AuthorizationDemandAttribute
    {
        /// <summary>
        /// Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        /// </summary>
        /// <param name="trueDemandAny_falseDemandAll">If true, user must me a member of *at least one* role. If false, user must be a member of *all* roles.</param>
        /// <param name="DemandedRoles">Collection of roles in order to authorize access to a class or method called using AOP proxy.</param>
        public DemandAspNetRoleAttribute(bool trueDemandAny_falseDemandAll, params string[] demandedRoles)
            : base(trueDemandAny_falseDemandAll, demandedRoles)
        {
        }

        /// <summary>
        /// Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        /// User must be a member of at least one demanded role in order to be authorized.
        /// </summary>
        /// <param name="DemandedRoles"></param>
        public DemandAspNetRoleAttribute(params string[] demandedRoles)
            : this(trueDemandAny_falseDemandAll: true, demandedRoles: demandedRoles)
        {
        }

        protected override IEnumerable<string> GetAuthorizedClaims()
        {
            string[] userRoles = Roles.GetRolesForUser();
            return userRoles;
        }

        public override System.Security.Principal.IIdentity Identity
        {
            get { return HttpContext.Current.User == null ? null : HttpContext.Current.User.Identity; }
        }
    }

    /// <summary>
    /// Enforces ASP.NET role membership authorization for methods or classes decorated with the DemandAspNetRoleAttribute.
    /// </summary>
    public class AspNetRoleAuthorizationAspect : AuthorizationAspect
    {
        protected override void ThrowAuthorizationException(string errorMsg)
        {
            try
            {
                HttpContext.Current.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            }
            catch
            {
                // No biggy if status code has already be written, we can skip this.
            }

            base.ThrowAuthorizationException(errorMsg);
        }
    }
}
