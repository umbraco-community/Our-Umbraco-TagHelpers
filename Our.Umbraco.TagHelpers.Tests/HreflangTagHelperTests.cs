#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/122
    public class HreflangTagHelperTests
    {
        private static readonly Guid PageKey = Guid.NewGuid();

        // en-US (default) and da: published; es: not published (the URL provider returns "#")
        private static readonly Dictionary<string, string> Urls = new()
        {
            ["en-US"] = "https://example.com/en/about/",
            ["da"] = "https://example.dk/om-os/",
            ["es"] = "#",
        };

        private static async Task<string> Render(string? xDefault = null, ContentVariation variation = ContentVariation.Culture, bool routedPage = true)
        {
            var contentType = Mock.Of<IPublishedContentType>(t => t.Variations == variation);
            var content = Mock.Of<IPublishedContent>(c => c.Key == PageKey && c.ContentType == contentType);
            var request = Mock.Of<IPublishedRequest>(r => r.PublishedContent == (routedPage ? content : null) && r.Uri == new Uri("https://example.com/en/about/"));
            var umbracoContext = Mock.Of<IUmbracoContext>(c => c.PublishedRequest == request);
            var umbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
            umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out umbracoContext)).Returns(true);

            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider
                .Setup(p => p.GetUrl(PageKey, UrlMode.Absolute, It.IsAny<string?>(), It.IsAny<Uri?>()))
                .Returns((Guid _, UrlMode _, string? culture, Uri? _) => Urls[culture!]);

            var languages = new[]
            {
                Mock.Of<ILanguage>(l => l.IsoCode == "en-US" && l.IsDefault),
                Mock.Of<ILanguage>(l => l.IsoCode == "da"),
                Mock.Of<ILanguage>(l => l.IsoCode == "es"),
            };
            var languageService = new Mock<ILanguageService>();
            languageService.Setup(s => s.GetAllAsync()).ReturnsAsync(languages);

            var tagHelper = new HreflangTagHelper(umbracoContextAccessor.Object, urlProvider.Object, languageService.Object) { XDefault = xDefault };
            var output = TestContextHelpers.GetTagHelperOutput("our-hreflang");
            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);
            ClassicAssert.IsNull(output.TagName, "the <our-hreflang> element itself is never rendered");
            return output.Content.GetContent();
        }

        private static string[] Lines(string html) => html.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        [Test]
        public async Task PublishedCultures_GetALinkEach_PlusXDefault()
        {
            CollectionAssert.AreEqual(new[]
            {
                "<link rel=\"alternate\" hreflang=\"en-US\" href=\"https://example.com/en/about/\" />",
                "<link rel=\"alternate\" hreflang=\"da\" href=\"https://example.dk/om-os/\" />",
                "<link rel=\"alternate\" hreflang=\"x-default\" href=\"https://example.com/en/about/\" />",
            }, Lines(await Render()));
        }

        [Test]
        public async Task XDefault_CanBeAnotherCulture()
        {
            StringAssert.Contains("<link rel=\"alternate\" hreflang=\"x-default\" href=\"https://example.dk/om-os/\" />", await Render(xDefault: "da"));
        }

        [Test]
        public async Task XDefault_CanBeLeftOut()
        {
            StringAssert.DoesNotContain("x-default", await Render(xDefault: "false"));
        }

        [Test]
        public async Task XDefault_IsLeftOut_WhenThatCultureIsntPublished()
        {
            StringAssert.DoesNotContain("x-default", await Render(xDefault: "es"));
        }

        [Test]
        public async Task InvariantContent_OutputsNothing()
        {
            ClassicAssert.AreEqual(string.Empty, await Render(variation: ContentVariation.Nothing));
        }

        [Test]
        public async Task NotAnUmbracoPage_OutputsNothing()
        {
            ClassicAssert.AreEqual(string.Empty, await Render(routedPage: false));
        }
    }
}
