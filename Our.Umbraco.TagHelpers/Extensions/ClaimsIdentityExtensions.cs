using System.Linq;
using System.Security.Claims;
using Umbraco.Cms.Core;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Extensions
{
    public static class ClaimsIdentityExtensions
    {
        public static bool IsAllowedToSeeEditLink(this ClaimsIdentity identity)
        {
            return IsLoggedIntoUmbraco(identity) && HasAccessToContentSection(identity);
        }

        public static bool IsLoggedIntoUmbraco(this ClaimsIdentity identity)
        {
            return identity?.AuthenticationType != null 
                && identity.AuthenticationType == Constants.Security.BackOfficeAuthenticationType;
        }

        public static bool HasAccessToContentSection(this ClaimsIdentity identity)
        {
            return identity?.Claims != null && identity.Claims.Any(x =>
                    x.Type == Constants.Security.AllowedApplicationsClaimType //TODO: OBSOLETE THING TO LOOK INTO
                    && x.Value == Constants.Conventions.PermissionCategories.ContentCategory);
        }
    }
}
