#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers.Tests
{
    // Root, Page A under it, Child under Page A: on a site with a culture domain /en on the root (like the demo
    // site), and on a site without domains (a single-language site, or one using the default URL scheme)
    public class ActiveClassTagHelperTests
    {
        private static readonly Guid Root = Guid.NewGuid();
        private static readonly Guid PageA = Guid.NewGuid();
        private static readonly Guid Child = Guid.NewGuid();

        private static readonly Dictionary<Guid, string> CultureDomainUrls = new()
        {
            [Root] = "https://example.com/en/",
            [PageA] = "https://example.com/en/page-a/",
            [Child] = "https://example.com/en/page-a/child/",
        };

        private static readonly Dictionary<Guid, string> NoDomainUrls = new()
        {
            [Root] = "https://example.com/",
            [PageA] = "https://example.com/page-a/",
            [Child] = "https://example.com/page-a/child/",
        };

        [TestCase("/en/page-a/", Description = "relative link to the current page")]
        [TestCase("https://example.com/en/page-a/", Description = "absolute link to the current page")]
        [TestCase("/en/page-a", Description = "without the trailing slash")]
        [TestCase("/EN/Page-A/?sort=1#top", Description = "different case, a query string and a fragment")]
        [TestCase("/en/", Description = "link to an ancestor")]
        public async Task Link_ToTheCurrentPageOrAnAncestor_IsActive(string href)
        {
            var output = await Render(currentPage: PageA, href);

            ClassicAssert.AreEqual("active", output.Attributes["class"]?.Value.ToString());
        }

        [TestCase("/en/page-a/child/", Description = "link to a child of the current page")]
        [TestCase("/en/page-b/", Description = "link to another page")]
        [TestCase("https://other.example.com/en/page-a/", Description = "same path on another host")]
        [TestCase("mailto:someone@example.com", Description = "not a page link")]
        public async Task Link_ToAnotherPage_IsNotActive(string href)
        {
            var output = await Render(currentPage: PageA, href);

            ClassicAssert.IsFalse(output.Attributes.ContainsName("class"));
        }

        [TestCase("/page-a/", Description = "relative link to the current page")]
        [TestCase("https://example.com/page-a/", Description = "absolute link to the current page")]
        [TestCase("/Page-A?sort=1", Description = "different case, no trailing slash and a query string")]
        [TestCase("/", Description = "link to the home page, an ancestor")]
        public async Task WithoutDomains_LinkToTheCurrentPageOrAnAncestor_IsActive(string href)
        {
            var output = await Render(currentPage: PageA, href, urls: NoDomainUrls, requestUri: "https://example.com/page-a/");

            ClassicAssert.AreEqual("active", output.Attributes["class"]?.Value.ToString());
        }

        [TestCase("/page-a/child/", Description = "link to a child of the current page")]
        [TestCase("/page-b/", Description = "link to another page")]
        [TestCase("/en/page-a/", Description = "the culture-domain URL of the current page")]
        public async Task WithoutDomains_LinkToAnotherPage_IsNotActive(string href)
        {
            var output = await Render(currentPage: PageA, href, urls: NoDomainUrls, requestUri: "https://example.com/page-a/");

            ClassicAssert.IsFalse(output.Attributes.ContainsName("class"));
        }

        [Test]
        public async Task InvariantContent_WithoutARequestCulture_IsActive()
        {
            var output = await Render(currentPage: PageA, href: "/page-a/", urls: NoDomainUrls, requestUri: "https://example.com/page-a/", culture: null);

            ClassicAssert.AreEqual("active", output.Attributes["class"]?.Value.ToString());
        }

        [Test]
        public async Task OurActiveHref_IsUsedInsteadOfTheHref()
        {
            var output = await Render(currentPage: Child, href: "/elsewhere/", activeLink: "/en/page-a/");

            ClassicAssert.AreEqual("active", output.Attributes["class"]?.Value.ToString());
        }

        [Test]
        public async Task TheTagHelperAttribute_IsRemovedFromTheMarkup()
        {
            var output = await Render(currentPage: PageA, href: "/en/page-b/");

            ClassicAssert.IsFalse(output.Attributes.ContainsName("our-active-class"));
        }

        private static async Task<TagHelperOutput> Render(
            Guid currentPage,
            string href,
            string? activeLink = null,
            Dictionary<Guid, string>? urls = null,
            string requestUri = "https://example.com/en/page-a/",
            string? culture = "en-US")
        {
            urls ??= CultureDomainUrls;
            var content = Mock.Of<IPublishedContent>(c => c.Key == currentPage);
            var request = Mock.Of<IPublishedRequest>(r =>
                r.PublishedContent == content
                && r.Uri == new Uri(requestUri)
                && r.Culture == culture);
            var umbracoContext = Mock.Of<IUmbracoContext>(c => c.PublishedRequest == request);

            var umbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
            umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out umbracoContext)).Returns(true);

            IEnumerable<Guid> ancestorsOrSelf = currentPage == Child ? new[] { Child, PageA, Root } : new[] { PageA, Root };
            var navigation = new Mock<IDocumentNavigationQueryService>();
            navigation.Setup(n => n.TryGetAncestorsOrSelfKeys(currentPage, out ancestorsOrSelf)).Returns(true);

            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider
                .Setup(p => p.GetUrl(It.IsAny<Guid>(), UrlMode.Absolute, culture, It.IsAny<Uri?>()))
                .Returns((Guid key, UrlMode _, string? _, Uri? _) => urls[key]);

            var tagHelper = new ActiveClassTagHelper(umbracoContextAccessor.Object, urlProvider.Object, navigation.Object)
            {
                ActiveClassName = "active",
                ActiveLink = activeLink,
            };

            var output = TestContextHelpers.GetTagHelperOutput("a", new TagHelperAttributeList
            {
                { "href", href },
                { "our-active-class", "active" },
            });
            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext(), output);
            return output;
        }
    }
}
