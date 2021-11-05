using System.Security.Claims;

namespace Our.Umbraco.TagHelpers.Services
{
    public interface IBackofficeUserAccessor
    {
        ClaimsIdentity BackofficeUser { get; }
    }
}
