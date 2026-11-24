#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Utils;
using Umbraco.Cms.Core.Cache;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/114
    // scope-styles="true" scopes the SVG's <style> to that SVG with a class on the <svg>
    public class InlineSvgScopeStylesTests
    {
        private const string WithStyle = "<svg><style>.st0{fill:#3544b1}</style><path class=\"st0\"></path></svg>";
        private const string WithoutStyle = "<svg><path class=\"st0\"></path></svg>";

        private static string Render(string svg, bool? scopeStyles, bool appSettings = false, bool ignoreAppSettings = false,
            string src = "/assets/logo.svg", string? cssClass = null, AppCaches? appCaches = null, bool cache = false, bool uniqueIds = false)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(() => Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes(svg))));
            var settings = new OurUmbracoTagHelpersConfiguration();
            settings.OurSVG.ScopeStyles = appSettings;

            var tagHelper = new InlineSvgTagHelper(null!, Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object), null!,
                Options.Create(settings), appCaches ?? AppCaches.Disabled, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = src,
                CssClass = cssClass,
                IgnoreAppSettings = ignoreAppSettings,
            };
            if (scopeStyles is bool value) tagHelper.ScopeStyles = value; // like Razor, only when the attribute is on the tag
            if (cache) tagHelper.Cache = true;
            if (uniqueIds) tagHelper.UniqueIds = true;

            var attributes = new TagHelperAttributeList();
            var output = new TagHelperOutput("our-svg", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelper.Process(new TagHelperContext(attributes, new Dictionary<object, object>(), "test"), output);
            return output.Content.GetContent();
        }

        private static readonly string LogoClass = SvgStyleScope.ScopeClass("/assets/logo.svg");

        [Test]
        public void TheSvgGetsTheScopeClass_AndItsSelectorsArePrefixed()
        {
            ClassicAssert.AreEqual(
                $"<svg class=\"{LogoClass}\"><style>.{LogoClass} .st0{{fill:#3544b1}}</style><path class=\"st0\"></path></svg>",
                Render(WithStyle, scopeStyles: true));
        }

        // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/115
        [Test]
        public void WithUniqueIdsToo_SelectorsAreScoped_AndTheIdsInThemArePrefixed()
        {
            const string svg = "<svg><style>#glow{opacity:.5} .st0{fill:url(#glow)}</style><filter id=\"glow\"></filter><path class=\"st0\"></path></svg>";

            var html = Render(svg, scopeStyles: true, uniqueIds: true);

            var prefix = Regex.Match(html, "id=\"(?<prefix>s[0-9a-f]{8}-)glow\"").Groups["prefix"].Value;
            ClassicAssert.IsNotEmpty(prefix, html);
            ClassicAssert.AreEqual(
                $"<svg class=\"{LogoClass}\"><style>.{LogoClass} #{prefix}glow{{opacity:.5}} .{LogoClass} .st0{{fill:url(#{prefix}glow)}}</style>" +
                $"<filter id=\"{prefix}glow\"></filter><path class=\"st0\"></path></svg>",
                html);
        }

        [Test]
        public void TheTagsOwnClass_IsKept()
        {
            StringAssert.StartsWith($"<svg class=\"logo {LogoClass}\">", Render(WithStyle, scopeStyles: true, cssClass: "logo"));
        }

        [Test]
        public void AnSvgWithoutStyle_IsUnchanged()
        {
            ClassicAssert.AreEqual(WithoutStyle, Render(WithoutStyle, scopeStyles: true));
        }

        [Test]
        public void TheSameFile_GetsTheSameClass_AndAnotherFileAnother()
        {
            var appCaches = new AppCaches(new ObjectCacheAppCache(), NoAppCache.Instance, new IsolatedCaches(_ => NoAppCache.Instance));

            ClassicAssert.AreEqual(Render(WithStyle, true, appCaches: appCaches, cache: true), Render(WithStyle, true, appCaches: appCaches, cache: true));
            StringAssert.Contains(SvgStyleScope.ScopeClass("/assets/footer.svg"), Render(WithStyle, true, src: "/assets/footer.svg"));
        }

        [Test]
        public void ScopingOneTag_DoesNotScopeTheCachedCopyForAnother()
        {
            var appCaches = new AppCaches(new ObjectCacheAppCache(), NoAppCache.Instance, new IsolatedCaches(_ => NoAppCache.Instance));

            Render(WithStyle, scopeStyles: true, appCaches: appCaches, cache: true);
            ClassicAssert.AreEqual(WithStyle, Render(WithStyle, scopeStyles: null, appCaches: appCaches, cache: true));
        }

        // (appsettings ScopeStyles, scope-styles attribute or null when not set, ignore-appsettings, expected scoped)
        [TestCase(false, null, false, false)]
        [TestCase(true, null, false, true)]
        [TestCase(true, null, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, true, true)]
        public void AttributeAndAppSettings(bool appSettings, bool? attribute, bool ignoreAppSettings, bool expectScoped)
        {
            ClassicAssert.AreEqual(expectScoped, Render(WithStyle, attribute, appSettings, ignoreAppSettings).Contains(LogoClass));
        }
    }
}
