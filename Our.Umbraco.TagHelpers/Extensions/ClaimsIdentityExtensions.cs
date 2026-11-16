using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Extensions
{
    public static class ClaimsIdentityExtensions
    {
        private const string ObsoleteMessage =
            "Umbraco 18 removed the allowed-sections claim, so this now loads the user on every call. "
            + "Get the user with IUserService.GetAsync(identity.GetUserKey()) and check that its AllowedSections contains "
            + "Constants.Applications.Content instead. This will be removed in a future major version.";

        /// <summary>
        /// Whether the identity is a backoffice user with access to the Content section.
        /// </summary>
        [Obsolete(ObsoleteMessage)]
        public static bool IsAllowedToSeeEditLink(this ClaimsIdentity identity)
        {
            return IsLoggedIntoUmbraco(identity) && HasAccessToContentSection(identity);
        }

        public static bool IsLoggedIntoUmbraco(this ClaimsIdentity identity)
        {
            return identity?.AuthenticationType != null 
                && identity.AuthenticationType == Constants.Security.BackOfficeAuthenticationType;
        }

        /// <summary>
        /// Whether the identity's user has access to the Content section.
        /// </summary>
        /// <remarks>
        /// Until 2.0.0 this read the allowed-sections claim, which Umbraco 18 removed. Kept for code written against
        /// 2.0.0, it now loads the user from the identity's key, as EditLinkTagHelper does. Extension methods can't
        /// take services, so it resolves IUserService from the current request (or the root provider).
        /// </remarks>
        [Obsolete(ObsoleteMessage)]
        public static bool HasAccessToContentSection(this ClaimsIdentity identity)
        {
            var userKey = identity?.GetUserKey();
            if (userKey is null)
            {
                return false;
            }

            var rootServices = StaticServiceProvider.Instance;
            var services = rootServices.GetService<IHttpContextAccessor>()?.HttpContext?.RequestServices ?? rootServices;
            var user = services.GetService<IUserService>()?.GetAsync(userKey.Value).GetAwaiter().GetResult();
            return user?.AllowedSections.Contains(Constants.Applications.Content) == true;
        }
    }
}
