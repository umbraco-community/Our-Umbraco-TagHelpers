using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Models;
using Our.Umbraco.TagHelpers.Services;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Tests
{
    [TestFixture]
    public class SelfHostTagHelperTests
    {
        [Test]
        public async Task ProcessAsync_SrcAttribute_SetsSrcAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.jpg" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.SrcAttribute = "https://example.com/test.jpg";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("img", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.AreEqual("/media/test.jpg", output.Attributes["src"].Value);
        }

        [Test]
        public async Task ProcessAsync_HrefAttribute_SetsHrefAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.pdf" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.HrefAttribute = "https://example.com/test.pdf";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("a", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.AreEqual("/media/test.pdf", output.Attributes["href"].Value);
        }

        [Test]
        public async Task ProcessAsync_RemovesOurSelfHostAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.jpg" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.SrcAttribute = "https://example.com/test.jpg";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("img", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.IsFalse(output.Attributes.ContainsName("our-self-host"));
        }

        [Test]
        public async Task ProcessAsync_RemovesFolderAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.jpg" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.SrcAttribute = "https://example.com/test.jpg";
            tagHelper.FolderName = "test-folder";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("img", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.IsFalse(output.Attributes.ContainsName("folder"));
        }

        [Test]
        public async Task ProcessAsync_RemovesExtAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.jpg" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.SrcAttribute = "https://example.com/test";
            tagHelper.Extension = "jpg";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("img", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.IsFalse(output.Attributes.ContainsName("ext"));
        }

        [Test]
        public async Task ProcessAsync_SrcAttribute_EnforcesExtAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.jpg" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.SrcAttribute = "https://example.com/test";
            tagHelper.Extension = "jpg";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("img", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.IsTrue(output.Attributes.ContainsName("src") && output.Attributes["src"].Value.ToString().EndsWith(tagHelper.Extension));
        }

        [Test]
        public async Task ProcessAsync_HrefAttribute_EnforcesExtAttribute()
        {
            // Arrange
            var selfHostServiceMock = new Mock<ISelfHostService>();
            selfHostServiceMock.Setup(x => x.SelfHostFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new SelfHostedFile { Url = "/media/test.pdf" });

            var tagHelper = new SelfHostTagHelper(selfHostServiceMock.Object);
            tagHelper.HrefAttribute = "https://example.com/test";
            tagHelper.Extension = "pdf";
            var id = "unique-id";
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var output = new TagHelperOutput("a", new TagHelperAttributeList(), (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.IsTrue(output.Attributes.ContainsName("href") && output.Attributes["href"].Value.ToString().EndsWith(tagHelper.Extension));
        }
    }
}
