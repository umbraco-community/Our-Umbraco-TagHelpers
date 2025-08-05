using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using System.Threading.Tasks;
using Our.Umbraco.TagHelpers.Tests.Helpers;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class VersionTagHelperTests
    {
        [Test]
        public async Task Process_WithNoAssemblyName_ReturnsEntryAssemblyVersion()
        {
            // Arrange
            var id = "version-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-version");

            var tagHelper = new VersionTagHelper();

            // Act
            await Task.Run(() => tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.IsNotNull(content);
            Assert.IsNotEmpty(content);
        }

        [Test]
        public async Task Process_WithValidAssemblyName_ReturnsAssemblyVersion()
        {
            // Arrange
            var id = "version-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-version");

            var tagHelper = new VersionTagHelper { AssemblyName = "System.Private.CoreLib" };

            // Act
            await Task.Run(() => tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.IsNotNull(content);
            Assert.IsNotEmpty(content);
        }

        [Test]
        public async Task Process_WithNonExistentAssemblyName_SuppressesOutput()
        {
            // Arrange
            var id = "version-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-version");

            var tagHelper = new VersionTagHelper { AssemblyName = "NonExistentAssembly" };

            // Act
            await Task.Run(() => tagHelper.Process(tagHelperContext, tagHelperOutput));

            // Assert - Should suppress output and have empty content
            var content = tagHelperOutput.Content.GetContent();
            Assert.IsTrue(string.IsNullOrEmpty(content));
        }
    }
}