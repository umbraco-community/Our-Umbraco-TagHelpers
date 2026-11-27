using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Configuration;
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
        private IOptions<OurUmbracoTagHelpersConfiguration> _settings = null!;

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

            var settings = new OurUmbracoTagHelpersConfiguration()
            {
                OurSVG =
                {
                    Cache = false,
                    EnsureViewBox = false,
                    CacheMinutes = 180
                }
            };
            _settings = Options.Create(settings);

        }

        [Test]
        public void NoOutputIfNoMediaOrFileSet()
        {
                    
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null);

            tagHelper.Process(_context, _output);

            Assert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfBothMediaAndFileSet()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null)
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
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null)
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
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null)
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
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null)
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
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, _settings, null)
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
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, _settings, null)
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
                _settings, 
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
                _settings, 
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
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null)
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            // The script is removed, and the link that was neutralised to syntax:error: is removed as well
            Assert.AreEqual("<a>Click here</a>end", _output.Content.GetContent());
        }

        // SVGs are inlined into the page, so anything in them that can run script must be removed (GHSA-rp8m-wpjx-frh9).
        // When something is removed the SVG is re-written by HtmlAgilityPack, which writes <x></x> for <x/>; browsers treat both the same.
        [TestCase("<svg onload=\"alert(1)\"><circle r=\"1\"/></svg>", "<svg><circle r=\"1\"></circle></svg>", Description = "event handler on the svg")]
        [TestCase("<svg><circle ONCLICK=\"alert(1)\" r=\"1\"/></svg>", "<svg><circle r=\"1\"></circle></svg>", Description = "event handler in upper case")]
        [TestCase("<svg><foreignObject><div>x</div></foreignObject><circle r=\"1\"/></svg>", "<svg><circle r=\"1\"></circle></svg>", Description = "foreignObject with HTML")]
        [TestCase("<svg><iframe src=\"https://example.com\"></iframe><embed src=\"x\"><object data=\"x\"></object><circle r=\"1\"/></svg>", "<svg><circle r=\"1\"></circle></svg>", Description = "iframe, embed and object")]
        [TestCase("<svg><a href=\"java&#x09;script:alert(1)\"><text>x</text></a></svg>", "<svg><a><text>x</text></a></svg>", Description = "script URL hidden with an entity")]
        [TestCase("<svg><a xlink:href=\"data:text/html,&lt;script&gt;alert(1)&lt;/script&gt;\"><text>x</text></a></svg>", "<svg><a><text>x</text></a></svg>", Description = "data URL in a link")]
        [TestCase("<svg><a><set attributeName=\"href\" to=\"javascript:alert(1)\"/><text>x</text></a></svg>", "<svg><a><text>x</text></a></svg>", Description = "set that turns a link into a script URL")]
        [TestCase("<svg><circle r=\"1\"><animate attributeName=\"onclick\" values=\"alert(1)\"/></circle></svg>", "<svg><circle r=\"1\"></circle></svg>", Description = "animate that targets an event handler")]
        [TestCase("<svg><use href=\"https://example.com/sprite.svg#icon\"/><use href=\"#icon\"/></svg>", "<svg><use></use><use href=\"#icon\"></use></svg>", Description = "external use reference")]
        [TestCase("<svg><image href=\"data:text/html,x\"/><image href=\"data:image/png;base64,AAAA\"/></svg>", "<svg><image></image><image href=\"data:image/png;base64,AAAA\"></image></svg>", Description = "image with a non-image data URL")]
        public void UnsafeSvg_IsCleaned(string svg, string expected)
        {
            Assert.AreEqual(expected, RenderSvg(svg));
        }

        [TestCase("<svg viewBox=\"0 0 10 10\"><defs><linearGradient id=\"g\"><stop offset=\"0\"/></linearGradient></defs><rect fill=\"url(#g)\" width=\"10\" height=\"10\"/></svg>", Description = "gradient referenced with url(#id)")]
        [TestCase("<svg><style>.st0{fill:#3544b1}</style><path class=\"st0\" d=\"M0 0h1v1z\"/></svg>", Description = "inline style block")]
        [TestCase("<svg><use href=\"#icon\"/><use xlink:href=\"#icon\"/></svg>", Description = "internal use references")]
        [TestCase("<svg><a href=\"https://umbraco.com\"><text>Umbraco</text></a><a href=\"/about\"><text>About</text></a><a href=\"mailto:hi@example.com\"><text>Mail</text></a></svg>", Description = "ordinary links")]
        [TestCase("<svg><image href=\"/media/photo.jpg\"/><image href=\"https://example.com/photo.png\"/></svg>", Description = "image references")]
        [TestCase("<svg><circle r=\"1\"><animate attributeName=\"r\" values=\"1;2\" dur=\"1s\"/></circle></svg>", Description = "ordinary animation")]
        public void SafeSvg_IsUnchanged(string svg)
        {
            Assert.AreEqual(svg, RenderSvg(svg));
        }

        private string RenderSvg(string svg)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes(svg))));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null)
            {
                FileSource = "/test.svg"
            };
            var output = new TagHelperOutput("our-svg", new TagHelperAttributeList(), (_, _) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelper.Process(_context, output);
            return output.Content.GetContent();
        }
    }
}
