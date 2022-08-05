using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Services
{

    /// <summary>
    /// Gets the backoffice user from the cookie
    /// Code from Markus Johansson on our.umbraco.com
    /// https://our.umbraco.com/forum/umbraco-9/106857-how-do-i-determine-if-a-backoffice-user-is-logged-in-from-a-razor-view#comment-334423
    /// </summary>
    public class BackofficeUserAccessor : IBackofficeUserAccessor
    {
        private readonly IOptionsSnapshot<CookieAuthenticationOptions> _cookieOptionsSnapshot;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BackofficeUserAccessor(
            IOptionsSnapshot<CookieAuthenticationOptions> cookieOptionsSnapshot,
            IHttpContextAccessor httpContextAccessor)
        {
            _cookieOptionsSnapshot = cookieOptionsSnapshot;
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsIdentity BackofficeUser
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;

                if (httpContext == null)
                    return new ClaimsIdentity();

                CookieAuthenticationOptions cookieOptions = _cookieOptionsSnapshot.Get(global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType);
                string? backOfficeCookie = httpContext.Request.Cookies[cookieOptions.Cookie.Name!];

                if (string.IsNullOrEmpty(backOfficeCookie))
                    return new ClaimsIdentity();

                AuthenticationTicket? unprotected = cookieOptions.TicketDataFormat.Unprotect(backOfficeCookie!);
                ClaimsIdentity backOfficeIdentity = unprotected!.Principal.GetUmbracoIdentity();

                return backOfficeIdentity;
            }
        }
    }
}