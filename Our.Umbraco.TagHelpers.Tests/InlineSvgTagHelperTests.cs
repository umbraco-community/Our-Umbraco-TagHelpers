using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Legacy;
using Umbraco.Cms.Core.Cache;
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
                    
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>());

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfBothMediaAndFileSet()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "test.svg",
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfFileNotSvg()
        {
            var tagHelper = new InlineSvgTagHelper(null, null, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "test.notsvg"
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfFileNotFound()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>(f => !f.Exists));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void ExpectedOutputIfValidFile()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes("test svg"))));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsNull(_output?.TagName);
            ClassicAssert.AreEqual(_output.Content.GetContent(), "test svg");
            ClassicAssert.IsFalse(_output.Attributes.ContainsName("src"));
            ClassicAssert.IsFalse(_output.Attributes.ContainsName("media-item"));
        }

        [Test]
        public void NoOutputIfMediaUrlNull()
        {
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns((string)null!);
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                MediaItem = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media)
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void NoOutputIfMediaNotSvg()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.notsvg");
            var tagHelper = new InlineSvgTagHelper(null, null, urlProvider.Object, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        // Umbraco 19 added a Lazy<ICoreScopeProvider> parameter to the MediaFileManager constructor
        private static MediaFileManager CreateMediaFileManager(IFileSystem fileSystem) =>
#if NET11_0_OR_GREATER
            new MediaFileManager(fileSystem, null, null, null, null, null);
#else
            new MediaFileManager(fileSystem, null, null, null, null);
#endif

        [Test]
        public void NoOutputIfMediaNotFound()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.svg");
            var fileSystem = Mock.Of<IFileSystem>(fs => !fs.FileExists(It.IsAny<string>()));
            var tagHelper = new InlineSvgTagHelper(
                CreateMediaFileManager(fileSystem),
                null,
                urlProvider.Object,
                _settings, 
                null, 
                Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsTrue(_output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void ExpectedOutputIfValidMedia()
        {
            var umbContent = Mock.Of<IPublishedContent>(c => c.ContentType.ItemType == PublishedItemType.Media);
            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider.Setup(p => p.GetMediaUrl(umbContent, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>())).Returns("test.svg");
            var fileSystem = Mock.Of<IFileSystem>(fs => fs.FileExists(It.IsAny<string>()) && fs.OpenFile(It.IsAny<string>()) == new MemoryStream(Encoding.UTF8.GetBytes("test svg")));
            var tagHelper = new InlineSvgTagHelper(
                CreateMediaFileManager(fileSystem),
                null,
                urlProvider.Object,
                _settings, 
                null, 
                Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                MediaItem = umbContent
            };

            tagHelper.Process(_context, _output);

            ClassicAssert.IsNull(_output?.TagName);
            ClassicAssert.AreEqual("test svg", _output.Content.GetContent());
            ClassicAssert.IsFalse(_output.Attributes.ContainsName("src"));
            ClassicAssert.IsFalse(_output.Attributes.ContainsName("media-item"));
        }

        [Test]
        public void SanitizesJavascript()
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes("<a xlink:href=\"javascript:alert('test');\">Click here</a><script attr=\"test\">test</script>end"))));
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
            var tagHelper = new InlineSvgTagHelper(null, hostEnv, null, _settings, null, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "test.svg"
            };

            tagHelper.Process(_context, _output);

            // The script is removed, and the link that was neutralised to syntax:error: is removed as well
            ClassicAssert.AreEqual("<a>Click here</a>end", _output.Content.GetContent());
        }

        // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/58
        [Test]
        public void CachedSvgKeepsEachTagsOwnClass()
        {
            var hostEnv = CreateHostEnvironment("<svg width=\"10\" height=\"10\"></svg>");
            var appCaches = CreateAppCaches();

            var first = RenderCachedSvg(hostEnv, appCaches, svg => svg.CssClass = "first");
            var second = RenderCachedSvg(hostEnv, appCaches, svg => svg.CssClass = "second");

            ClassicAssert.AreEqual("<svg width=\"10\" height=\"10\" class=\"first\"></svg>", first);
            ClassicAssert.AreEqual("<svg width=\"10\" height=\"10\" class=\"second\"></svg>", second);
        }

        [Test]
        public void CachedSvgKeepsEachTagsOwnViewBoxSetting()
        {
            var hostEnv = CreateHostEnvironment("<svg width=\"10\" height=\"10\"></svg>");
            var appCaches = CreateAppCaches();

            var withViewBox = RenderCachedSvg(hostEnv, appCaches, svg => svg.EnsureViewBox = true);
            var withoutViewBox = RenderCachedSvg(hostEnv, appCaches, svg => { });

            ClassicAssert.AreEqual("<svg viewbox=\"0 0 10 10\"></svg>", withViewBox);
            ClassicAssert.AreEqual("<svg width=\"10\" height=\"10\"></svg>", withoutViewBox);
        }

        // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/57
        // Each test renders the file, changes it, and renders again: the second render shows whether it was cached
        // All 12 combinations, in the same order as the "When is an SVG cached?" table on the docs site's our-svg page:
        // (appsettings Cache, cache attribute or null when not set, ignore-appsettings, expected cached)
        [TestCase(false, null, false, false)]
        [TestCase(true, null, false, true)]
        [TestCase(false, null, true, false)]
        [TestCase(true, null, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, true, false, true)]
        [TestCase(false, true, true, true)]
        [TestCase(true, true, true, true)]
        [TestCase(false, false, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(false, false, true, false)]
        [TestCase(true, false, true, false)]
        public void Cache_AttributeAndAppSettings(bool appSettingsCache, bool? cacheAttribute, bool ignoreAppSettings, bool expectCached)
        {
            var settings = new OurUmbracoTagHelpersConfiguration();
            settings.OurSVG.Cache = appSettingsCache;
            var svg = "<svg>first</svg>";
            var hostEnv = CreateHostEnvironment(() => svg);
            var appCaches = CreateAppCaches();

            string Render()
            {
                var tagHelper = new InlineSvgTagHelper(null!, hostEnv, null!, Options.Create(settings), appCaches, Mock.Of<ILogger<InlineSvgTagHelper>>())
                {
                    FileSource = "/test.svg",
                    IgnoreAppSettings = ignoreAppSettings,
                };
                // Like Razor, only set Cache when the tag has a cache attribute
                if (cacheAttribute is bool cache)
                {
                    tagHelper.Cache = cache;
                }
                var output = new TagHelperOutput("our-svg", new TagHelperAttributeList(), (_, _) =>
                    Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
                tagHelper.Process(_context, output);
                return output.Content.GetContent();
            }

            ClassicAssert.AreEqual("<svg>first</svg>", Render());
            svg = "<svg>changed</svg>";
            ClassicAssert.AreEqual(expectCached ? "<svg>first</svg>" : "<svg>changed</svg>", Render());
        }

        private static IWebHostEnvironment CreateHostEnvironment(Func<string> svg)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(() => Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes(svg()))));
            return Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
        }

        // SVGs are inlined into the page, so anything in them that can run script must be removed.
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
            ClassicAssert.AreEqual(expected, RenderSvg(svg));
        }

        [TestCase("<svg viewBox=\"0 0 10 10\"><defs><linearGradient id=\"g\"><stop offset=\"0\"/></linearGradient></defs><rect fill=\"url(#g)\" width=\"10\" height=\"10\"/></svg>", Description = "gradient referenced with url(#id)")]
        [TestCase("<svg><style>.st0{fill:#3544b1}</style><path class=\"st0\" d=\"M0 0h1v1z\"/></svg>", Description = "inline style block")]
        [TestCase("<svg><use href=\"#icon\"/><use xlink:href=\"#icon\"/></svg>", Description = "internal use references")]
        [TestCase("<svg><a href=\"https://umbraco.com\"><text>Umbraco</text></a><a href=\"/about\"><text>About</text></a><a href=\"mailto:hi@example.com\"><text>Mail</text></a></svg>", Description = "ordinary links")]
        [TestCase("<svg><image href=\"/media/photo.jpg\"/><image href=\"https://example.com/photo.png\"/></svg>", Description = "image references")]
        [TestCase("<svg><circle r=\"1\"><animate attributeName=\"r\" values=\"1;2\" dur=\"1s\"/></circle></svg>", Description = "ordinary animation")]
        public void SafeSvg_IsUnchanged(string svg)
        {
            ClassicAssert.AreEqual(svg, RenderSvg(svg));
        }

        // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/116
        // Attributes on <our-svg> that aren't the tag helper's own are copied onto the root <svg>
        [Test]
        public void OtherAttributes_AreCopiedOntoTheSvg()
        {
            var svg = RootSvg(RenderSvg("<svg viewBox=\"0 0 1 1\"><circle r=\"1\"/></svg>", new TagHelperAttributeList
            {
                { "aria-hidden", "true" },
                { "id", "logo" },
                { "data-track", "header-logo" },
            }));

            ClassicAssert.AreEqual("true", svg.GetAttributeValue("aria-hidden", null));
            ClassicAssert.AreEqual("logo", svg.GetAttributeValue("id", null));
            ClassicAssert.AreEqual("header-logo", svg.GetAttributeValue("data-track", null));
            ClassicAssert.AreEqual("0 0 1 1", svg.GetAttributeValue("viewBox", null));
        }

        [Test]
        public void OtherAttributes_OverrideTheFilesOwnValues()
        {
            var svg = RootSvg(RenderSvg("<svg width=\"10\" height=\"10\"></svg>", new TagHelperAttributeList { { "width", "20" } }));

            ClassicAssert.AreEqual("20", svg.GetAttributeValue("width", null));
            ClassicAssert.AreEqual("10", svg.GetAttributeValue("height", null));
            ClassicAssert.AreEqual(1, svg.Attributes.Count(a => a.Name == "width"));
        }

        [Test]
        public void OtherAttributes_AreHtmlEncoded()
        {
            var html = RenderSvg("<svg></svg>", new TagHelperAttributeList { { "aria-label", "Logo \"><script>alert(1)</script> & co" } });
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            ClassicAssert.IsNull(doc.DocumentNode.SelectSingleNode("//script"));
            ClassicAssert.AreEqual("Logo \"><script>alert(1)</script> & co", HtmlAgilityPack.HtmlEntity.DeEntitize(RootSvg(html).GetAttributeValue("aria-label", null)));
        }

        [Test]
        public void OtherAttributes_FromRazorExpressions_AreKeptEncoded()
        {
            // An attribute written with a Razor expression arrives as already-encoded HTML content
            var svg = RootSvg(RenderSvg("<svg></svg>", new TagHelperAttributeList { { "aria-label", new HtmlString("Logo &amp; tagline") } }));

            ClassicAssert.AreEqual("Logo & tagline", HtmlAgilityPack.HtmlEntity.DeEntitize(svg.GetAttributeValue("aria-label", null)));
        }

        [Test]
        public void MinimizedAttribute_IsCopied()
        {
            var svg = RootSvg(RenderSvg("<svg></svg>", new TagHelperAttributeList { new TagHelperAttribute("hidden") }));

            ClassicAssert.IsTrue(svg.Attributes.Contains("hidden"));
        }

        private static HtmlAgilityPack.HtmlNode RootSvg(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.SelectSingleNode("//svg");
        }

        private string RenderSvg(string svg, TagHelperAttributeList? attributes = null)
        {
            var tagHelper = new InlineSvgTagHelper(null!, CreateHostEnvironment(svg), null!, _settings, null!, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "/test.svg",
            };
            var output = new TagHelperOutput("our-svg", attributes ?? new TagHelperAttributeList(), (_, _) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelper.Process(_context, output);
            return output.Content.GetContent();
        }

        private static IWebHostEnvironment CreateHostEnvironment(string svg)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider
                .Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(() => Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes(svg))));
            return Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object);
        }

        private static AppCaches CreateAppCaches() =>
            new(new ObjectCacheAppCache(), NoAppCache.Instance, new IsolatedCaches(_ => NoAppCache.Instance));

        private string RenderCachedSvg(IWebHostEnvironment hostEnv, AppCaches appCaches, Action<InlineSvgTagHelper> configure)
        {
            var tagHelper = new InlineSvgTagHelper(null!, hostEnv, null!, _settings, appCaches, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "/test.svg",
                Cache = true
            };
            configure(tagHelper);

            var output = new TagHelperOutput("our-svg", new TagHelperAttributeList(), (_, _) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelper.Process(_context, output);
            return output.Content.GetContent();
        }
    }
}
