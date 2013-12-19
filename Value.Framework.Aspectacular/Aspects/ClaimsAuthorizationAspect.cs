using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Value.Framework.Core;

namespace Value.Framework.Aspectacular.Aspects
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class DemandClaimsAttribute : Attribute
    {
        public bool DemandAny { get; protected set; }
        public string[] DemandedClaims { get; protected set; }

        protected readonly Type claimsIdentityType;

        public DemandClaimsAttribute(Type claimsIdentityType, bool trueDemandAny_falseDemandAll, params string[] demandedClaims)
        {
            if (claimsIdentityType == null)
                throw new ArgumentNullException("claimsIdentityType");

            if (!claimsIdentityType.IsSubclassOf(typeof(ClaimsPrincipal)))
                throw new Exception("Type {0} must inherit ClaimsPrincipal to be used for authorization.".SmartFormat(claimsIdentityType.FormatCSharp()));

            this.claimsIdentityType = claimsIdentityType;
            this.DemandAny = trueDemandAny_falseDemandAll;
            this.DemandedClaims = demandedClaims;
        }

        public IEnumerable<string> GetAuthorizedClaims()
        {
            if (ClaimsPrincipal.Current == null)
                return null;

            ClaimsIdentity identity = ClaimsPrincipal.Current.Identities.Where(id => id.GetType() == this.claimsIdentityType).SingleOrDefault();
            if (identity == null)
                return null;

            IEnumerable<string> authorizedCalims = identity.Claims.Select(claim => claim.Value);
            return authorizedCalims;
        }

        public bool IsAuthorized()
        {
            if(DemandedClaims.IsNullOrEmpty())
                return true;

            IEnumerable<string> authorizedClaims = this.GetAuthorizedClaims();
            if(authorizedClaims == null)
                return false;

            int authorizedClaimCount = authorizedClaims.Where(claim => this.DemandedClaims.Contains(claim)).Count();

            if (this.DemandAny)
                return authorizedClaimCount > 0;

            // Demand all.
            return authorizedClaimCount == this.DemandedClaims.Length;
        }
    }

    //public class ClaimsAuthorizationAspect : Aspect
    //{
    //}
}
