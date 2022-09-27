using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class InlineSvgTagHelperTests
    {
        private TagHelperContext _context = null!;
        private TagHelperOutput _output = null!;

        [SetUp]
        public void Setup()
        {
            var attributes = new TagHelperAttributeList
            {
                { "src", "test-src" },
                { "media-item", "test-media" }
            };
            _context = new TagHelperContext(attributes, new Dictionary<object, object>(), "test");
            _output = new TagHelperOutput("umb-svg", attributes, (result, encoder) =>
            {
                var content = new DefaultTagHelperContent();
                content.SetContent("Something else");
                return Task.FromResult<TagHelperContent>(content);
            });
        }

        [Test]
        public void NoOutputIfNoMediaOrFileSet()
        {
            var tagHelper = new InlineSvgTagHelper(null, null, null, null, null);

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfBothMediaAndFileSet()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var tagHelper = new InlineSvgTagHelper(null, null, null, null, null)
            {
                FileSource = "test.svg",
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfFileNotSvg()
        {
            var tagHelper = new InlineSvgTagHelper(null, null, null, null, null)
            {
                FileSource = "test.notsvg"
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfFileNotFound()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>(f => !f.Exists));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, null, null)
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void ExpectedOutputIfValidFile()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes("test svg"))));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, null, null)
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            Assert.IsNull(_output?.TagName);
            Assert.AreEqual(_output.Content.GetContent(), "test svg");
            Assert.IsFalse(_output.Attributes.ContainsName("src"));
            Assert.IsFalse(_output.Attributes.ContainsName("media-item"));
        }

        [Test]
        public void NoOutputIfMediaUrlNull()
        {
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns((string)null!);
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, null, null)
            {
                MediaItem = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media)
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfMediaNotSvg()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.notsvg");
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, null, null)
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfMediaNotFound()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.svg");
            var fileSystem = Mock.Of<IFileSystem>(fs => !fs.FileExists(It.IsAny<string>()));
            var tagHelper = new InlineSvgTagHelper(
                new MediaFileManager(fileSystem, null, null, null, null, Mock.Of<IOptions<ContentSettings>>()),
                null,
                urlProvider.Object, 
                null, 
                null)
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void ExpectedOutputIfValidMedia()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.svg");
            var fileSystem = Mock.Of<IFileSystem>(fs => fs.FileExists(It.IsAny<string>()) && fs.OpenFile(It.IsAny<string>()) == new MemoryStream(Encoding.UTF8.GetBytes("test svg")));
            var tagHelper = new InlineSvgTagHelper(
                new MediaFileManager(fileSystem, null, null, null, null, Mock.Of<IOptions<ContentSettings>>()),
                null,
                urlProvider.Object, 
                null, 
                null)
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            Assert.IsNull(_output?.TagName);
            Assert.AreEqual("test svg", _output.Content.GetContent());
            Assert.IsFalse(_output.Attributes.ContainsName("src"));
            Assert.IsFalse(_output.Attributes.ContainsName("media-item"));
        }

        [Test]
        public void SanitizesJavascript()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes("<a xlink:href=\"javascript:alert('test');\">Click here</a><script attr=\"test\">test</script>end"))));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, null, null)
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            Assert.AreEqual("<a xlink:href=\"syntax:error:alert('test');\">Click here</a>end", _output.Content.GetContent());
        }
    }
}
