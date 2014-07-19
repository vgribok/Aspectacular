#region License Info Header

// This file is a part of the Aspectacular framework created by Vlad Hrybok.
// This software is free and is distributed under MIT License: http://bit.ly/Q3mUG7

#endregion

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Web;
using System.Web.Security;

namespace Aspectacular
{
    /// <summary>
    ///     Decorate methods and classes with this attribute.
    ///     Caution: Uses Roles.GetRolesForUser() internally, which has weird caching policy.
    /// </summary>
    /// <remarks>
    ///     For better performance, implement your own ClaimsIdentity for ASP.NET Roles
    ///     that caches claims for longer periods of time, and then use DemandClaimsAttribute instead
    ///     of DemandAspNetRoleAttribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class DemandAspNetRoleAttribute : AuthorizationDemandAttribute
    {
        /// <summary>
        ///     Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        /// </summary>
        /// <param name="trueDemandAny_falseDemandAll">
        ///     If true, user must me a member of *at least one* role. If false, user must
        ///     be a member of *all* roles.
        /// </param>
        /// <param name="demandedRoles">
        ///     Collection of roles in order to authorize access to a class or method called using AOP
        ///     proxy.
        /// </param>
        public DemandAspNetRoleAttribute(bool trueDemandAny_falseDemandAll, params string[] demandedRoles)
            : base(trueDemandAny_falseDemandAll, demandedRoles)
        {
        }

        /// <summary>
        ///     Marks methods and classes as requiring current user to the member of one or more ASP.NET security roles.
        ///     User must be a member of at least one demanded role in order to be authorized.
        /// </summary>
        /// <param name="demandedRoles"></param>
        public DemandAspNetRoleAttribute(params string[] demandedRoles)
            : this(true, demandedRoles)
        {
        }

        protected override IEnumerable<string> GetAuthorizedClaims()
        {
            string[] userRoles = Roles.GetRolesForUser();
            return userRoles;
        }

        public override IIdentity Identity
        {
            get { return HttpContext.Current.User == null ? null : HttpContext.Current.User.Identity; }
        }
    }

    /// <summary>
    ///     Enforces ASP.NET role membership authorization for methods or classes decorated with the DemandAspNetRoleAttribute.
    /// </summary>
    public class AspNetRoleAuthorizationAspect : AuthorizationAspect
    {
        protected override void ThrowAuthorizationException(string errorMsg)
        {
            try
            {
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // No biggy if status code has already been written, we can skip this.
            }

            base.ThrowAuthorizationException(errorMsg);
        }
    }
}