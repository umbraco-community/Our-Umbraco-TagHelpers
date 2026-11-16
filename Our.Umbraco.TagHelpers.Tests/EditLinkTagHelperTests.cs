#nullable enable

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Services;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class EditLinkTagHelperTests
    {
        private static readonly Guid _userKey = Guid.NewGuid();
        private const string _contentKey = "b0c59e4c-158c-4fd4-9beb-ebe907693f1c";

        private static ClaimsIdentity BackofficeIdentity(Guid? userKey)
        {
            var identity = new ClaimsIdentity(Constants.Security.BackOfficeAuthenticationType);
            if (userKey.HasValue)
            {
                // Umbraco stores the user key in the OpenID "sub" claim (see AddRequiredClaims)
                identity.AddClaim(new Claim("sub", userKey.Value.ToString()));
            }
            return identity;
        }

        private static EditLinkTagHelper CreateTagHelper(ClaimsIdentity identity, IUser? user)
        {
            var accessor = new Mock<IBackofficeUserAccessor>();
            accessor.Setup(x => x.BackofficeUser).Returns(identity);

            var userService = new Mock<IUserService>();
            userService.Setup(x => x.GetAsync(_userKey)).ReturnsAsync(user);

            return new EditLinkTagHelper(accessor.Object, Mock.Of<IUmbracoContextAccessor>(), userService.Object)
            {
                ContentKey = _contentKey
            };
        }

        private static IUser UserWithSections(params string[] sections)
        {
            var user = new Mock<IUser>();
            user.Setup(x => x.AllowedSections).Returns(sections);
            return user.Object;
        }

        [Test]
        public async Task Renders_Link_When_User_Has_Content_Section()
        {
            var tagHelper = CreateTagHelper(BackofficeIdentity(_userKey), UserWithSections(Constants.Applications.Content));
            var output = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);

            Assert.That(output.TagName, Is.EqualTo("a"));
            Assert.That(output.Attributes["href"].Value,
                Is.EqualTo($"/umbraco/section/content/workspace/document/edit/{_contentKey}"));
        }

        [Test]
        public async Task Suppressed_When_User_Lacks_Content_Section()
        {
            var tagHelper = CreateTagHelper(BackofficeIdentity(_userKey), UserWithSections(Constants.Applications.Media));
            var output = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);

            Assert.That(output.TagName, Is.Null);
        }

        [Test]
        public async Task Suppressed_When_Not_Logged_Into_Backoffice()
        {
            var tagHelper = CreateTagHelper(new ClaimsIdentity(), UserWithSections(Constants.Applications.Content));
            var output = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);

            Assert.That(output.TagName, Is.Null);
        }

        [Test]
        public async Task Suppressed_When_Identity_Has_No_User_Key()
        {
            var tagHelper = CreateTagHelper(BackofficeIdentity(null), UserWithSections(Constants.Applications.Content));
            var output = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);

            Assert.That(output.TagName, Is.Null);
        }

        [Test]
        public async Task Suppressed_When_User_Not_Found()
        {
            var tagHelper = CreateTagHelper(BackofficeIdentity(_userKey), null);
            var output = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);

            Assert.That(output.TagName, Is.Null);
        }
    }
}
