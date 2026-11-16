using Microsoft.AspNetCore.Hosting;
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

            ClassicAssert.AreEqual("<a xlink:href=\"syntax:error:alert('test');\">Click here</a>end", _output.Content.GetContent());
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
        // All 12 combinations, in the same order as the README's "When is an SVG cached?" table:
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
