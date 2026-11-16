#nullable enable
#pragma warning disable CS0618 // These tests cover the obsolete methods, kept so 2.0.0 code keeps working
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.TagHelpers.Tests
{
    // Umbraco 18 removed the allowed-sections claim these methods used to read, so they now look the user up:
    // they must keep working for code written against 2.0.0, on every supported Umbraco version
    public class ClaimsIdentityExtensionsTests
    {
        private static readonly Guid EditorKey = Guid.NewGuid();
        private static readonly Guid MediaOnlyKey = Guid.NewGuid();

        private IServiceProvider? _originalServiceProvider;

        [SetUp]
        public void SetUp()
        {
            _originalServiceProvider = StaticServiceProvider.Instance;
            StaticServiceProvider.Instance = Services(UserService());
        }

        [TearDown]
        public void TearDown() => StaticServiceProvider.Instance = _originalServiceProvider!;

        [Test]
        public void HasAccessToContentSection_IsTrue_ForAUserWithTheContentSection()
        {
            ClassicAssert.IsTrue(Identity(EditorKey).HasAccessToContentSection());
        }

        [Test]
        public void HasAccessToContentSection_IsFalse_ForAUserWithoutTheContentSection()
        {
            ClassicAssert.IsFalse(Identity(MediaOnlyKey).HasAccessToContentSection());
        }

        [Test]
        public void HasAccessToContentSection_IsFalse_ForAnUnknownUser()
        {
            ClassicAssert.IsFalse(Identity(Guid.NewGuid()).HasAccessToContentSection());
        }

        [Test]
        public void HasAccessToContentSection_IsFalse_WithoutAUserKey()
        {
            ClassicAssert.IsFalse(new ClaimsIdentity(Array.Empty<Claim>(), Constants.Security.BackOfficeAuthenticationType).HasAccessToContentSection());
        }

        [Test]
        public void IsAllowedToSeeEditLink_IsTrue_ForABackofficeUserWithTheContentSection()
        {
            ClassicAssert.IsTrue(Identity(EditorKey).IsAllowedToSeeEditLink());
        }

        [Test]
        public void IsAllowedToSeeEditLink_IsFalse_WhenNotLoggedIntoTheBackoffice()
        {
            ClassicAssert.IsFalse(Identity(EditorKey, authenticationType: "Cookies").IsAllowedToSeeEditLink());
        }

        [Test]
        public void IsAllowedToSeeEditLink_IsFalse_ForABackofficeUserWithoutTheContentSection()
        {
            ClassicAssert.IsFalse(Identity(MediaOnlyKey).IsAllowedToSeeEditLink());
        }

        [Test]
        public void TheCurrentRequestsServices_AreUsed_WhenThereIsARequest()
        {
            // The root provider knows nobody; the request's services know the editor
            var requestServices = Services(UserService());
            var httpContextAccessor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == new DefaultHttpContext { RequestServices = requestServices });
            StaticServiceProvider.Instance = Services(Mock.Of<IUserService>(), httpContextAccessor);

            ClassicAssert.IsTrue(Identity(EditorKey).HasAccessToContentSection());
        }

        private static ClaimsIdentity Identity(Guid userKey, string authenticationType = Constants.Security.BackOfficeAuthenticationType) =>
            new(new[] { new Claim("sub", userKey.ToString()) }, authenticationType);

        private static IUserService UserService()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.GetAsync(EditorKey)).ReturnsAsync(Mock.Of<IUser>(u => u.AllowedSections == new[] { "content", "media" }));
            userService.Setup(s => s.GetAsync(MediaOnlyKey)).ReturnsAsync(Mock.Of<IUser>(u => u.AllowedSections == new[] { "media" }));
            return userService.Object;
        }

        private static IServiceProvider Services(IUserService userService, IHttpContextAccessor? httpContextAccessor = null)
        {
            var services = new ServiceCollection().AddSingleton(userService);
            if (httpContextAccessor is not null)
            {
                services.AddSingleton(httpContextAccessor);
            }
            return services.BuildServiceProvider();
        }
    }
}
