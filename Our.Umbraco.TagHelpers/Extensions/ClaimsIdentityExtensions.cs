using System.Security.Claims;
using Umbraco.Cms.Core;

namespace Our.Umbraco.TagHelpers.Extensions
{
    public static class ClaimsIdentityExtensions
    {
        public static bool IsLoggedIntoUmbraco(this ClaimsIdentity identity)
        {
            return identity?.AuthenticationType != null 
                && identity.AuthenticationType == Constants.Security.BackOfficeAuthenticationType;
        }
    }
}
