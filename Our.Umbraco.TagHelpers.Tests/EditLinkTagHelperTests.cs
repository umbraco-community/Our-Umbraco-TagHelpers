using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Our.Umbraco.TagHelpers.Services;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class EditLinkTagHelperTests
    {
        [Test]
        public void Constructor_WithValidParameters_InitializesProperties()
        {
            // Arrange
            var mockBackofficeUserAccessor = new Mock<IBackofficeUserAccessor>();
            var mockUmbracoContextAccessor = new Mock<IUmbracoContextAccessor>();

            // Act
            var tagHelper = new EditLinkTagHelper(mockBackofficeUserAccessor.Object, mockUmbracoContextAccessor.Object);

            // Assert
            Assert.IsNotNull(tagHelper);
            Assert.AreEqual("/umbraco#/content/content/edit/", tagHelper.EditUrl);
            Assert.IsFalse(tagHelper.UseDefaultStyles);
            Assert.AreEqual(int.MinValue, tagHelper.ContentId);
        }

        [Test]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var mockBackofficeUserAccessor = new Mock<IBackofficeUserAccessor>();
            var mockUmbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
            var tagHelper = new EditLinkTagHelper(mockBackofficeUserAccessor.Object, mockUmbracoContextAccessor.Object);

            // Act
            tagHelper.ContentId = 1234;
            tagHelper.EditUrl = "/custom/edit/url/";
            tagHelper.UseDefaultStyles = true;

            // Assert
            Assert.AreEqual(1234, tagHelper.ContentId);
            Assert.AreEqual("/custom/edit/url/", tagHelper.EditUrl);
            Assert.IsTrue(tagHelper.UseDefaultStyles);
        }

        [Test]
        public void Process_WithNoBackofficeUser_SuppressesOutput()
        {
            // Arrange
            var mockBackofficeUserAccessor = new Mock<IBackofficeUserAccessor>();
            var mockUmbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
            
            // Setup: No backoffice user (null)
            mockBackofficeUserAccessor.Setup(x => x.BackofficeUser).Returns((System.Security.Claims.ClaimsIdentity)null);

            var tagHelper = new EditLinkTagHelper(mockBackofficeUserAccessor.Object, mockUmbracoContextAccessor.Object);
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("edit-link-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-edit-link");

            // Act
            tagHelper.Process(tagHelperContext, tagHelperOutput);

            // Assert - When no backoffice user, output should be suppressed (no tag name)
            // The EditLinkTagHelper should suppress output when no backoffice user is found
            Assert.IsNull(tagHelperOutput.TagName);
        }
    }
}