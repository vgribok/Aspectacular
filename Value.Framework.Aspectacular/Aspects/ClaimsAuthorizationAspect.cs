using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Aspectacular
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public abstract class AuthorizationDemandAttribute : Attribute
    {
        public bool DemandAny { get; protected set; }
        public string[] DemandedClaims { get; protected set; }

        protected AuthorizationDemandAttribute(bool trueDemandAny_falseDemandAll, string[] demandedClaims)
        {
            if (demandedClaims.IsNullOrEmpty())
                throw new ArgumentNullException("demandedClaims collection cannot be empty.");

            this.DemandAny = trueDemandAny_falseDemandAll;
            this.DemandedClaims = demandedClaims;
        }

        public abstract IIdentity Identity
        {
            get;
        }

        public bool IsAuthorized()
        {
            if (DemandedClaims.IsNullOrEmpty())
                return true;

            return this.IsAuthorizedInternal();
        }

        protected virtual bool IsAuthorizedInternal()
        {
            IEnumerable<string> authorizedClaims = this.GetAuthorizedClaims();
            if (authorizedClaims == null)
                return false;

            int authorizedClaimCount = authorizedClaims.Where(claim => this.DemandedClaims.Contains(claim)).Count();

            if (this.DemandAny)
                return authorizedClaimCount > 0;

            // Demand all.
            return authorizedClaimCount == this.DemandedClaims.Length;
        }

        protected abstract IEnumerable<string> GetAuthorizedClaims();
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class DemandClaimsAttribute : AuthorizationDemandAttribute
    {
        protected readonly Type claimsIdentityType;

        private readonly Lazy<ClaimsIdentity> identity;

        public DemandClaimsAttribute(Type claimsIdentityType, params string[] demandedClaims)
            : this(claimsIdentityType, true, demandedClaims)
        {
        }

        public DemandClaimsAttribute(Type claimsIdentityType, bool trueDemandAny_falseDemandAll, params string[] demandedClaims)
            : base(trueDemandAny_falseDemandAll, demandedClaims)
        {
            if (claimsIdentityType == null)
                throw new ArgumentNullException("claimsIdentityType");

            if (!claimsIdentityType.IsSubclassOf(typeof(ClaimsPrincipal)))
                throw new Exception("Type {0} must inherit ClaimsPrincipal to be used for authorization.".SmartFormat(claimsIdentityType.FormatCSharp()));

            this.claimsIdentityType = claimsIdentityType;

            this.identity = new Lazy<ClaimsIdentity>(() => ClaimsPrincipal.Current == null ? null : ClaimsPrincipal.Current.Identities.Where(id => id.GetType() == this.claimsIdentityType).SingleOrDefault());
        }

        public override IIdentity Identity
        {
            get { return this.identity.Value; }
        }

        public ClaimsIdentity ClaimsIdentity
        {
            get { return this.identity.Value; }
        }

        protected override IEnumerable<string> GetAuthorizedClaims()
        {
            if (this.ClaimsIdentity == null)
                return null;

            IEnumerable<string> authorizedClaims = this.ClaimsIdentity.Claims.Where(claim => claim.Type == this.ClaimsIdentity.RoleClaimType).Select(claim => claim.Value);
            return authorizedClaims;
        }

        protected override bool IsAuthorizedInternal()
        {
            if (!this.Identity.IsAuthenticated)
                return false;

            return base.IsAuthorizedInternal();
        }
    }

    /// <summary>
    /// Enforces claims/roles authorization for methods or classes decorated with subclasses of the AuthorizationDemandAttribute.
    /// </summary>
    public class AuthorizationAspect : Aspect
    {
        public override void Step_2_BeforeTryingMethodExec()
        {
            this.EnsureAuthorization();
            this.Log(EntryType.Warning, "Authorized", true.ToString());
        }

        protected void EnsureAuthorization()
        {
            AuthorizationDemandAttribute claimDemandAttrib = this.Proxy.InterceptedCallMetaData.GetMethodOrClassAttribute<AuthorizationDemandAttribute>();

            this.LogInformationWithKey("Demanded claims/roles", "{0}: {1}",
                                    claimDemandAttrib.DemandAny ? "ANY" : "ALL",
                                    string.Join(", ", claimDemandAttrib.DemandedClaims)
                                    );

            if (claimDemandAttrib == null)
                return;

            if (claimDemandAttrib.IsAuthorized())
                return;

            this.Log(EntryType.Warning, "Authorized", false.ToString());

            string errorMsg = string.Format("User \"{0}\" is not authorized to call this function.", claimDemandAttrib.Identity.Name);

            this.Log(EntryType.Warning, "Authorization Failed", errorMsg);

            this.ThrowAuthorizationException(errorMsg);
        }

        protected virtual void ThrowAuthorizationException(string errorMsg)
        {
            throw new Exception(errorMsg);
        }
    }
}
