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
    // The demo site's layout: a culture domain /en on the root, Page A under it, Child under Page A
    public class ActiveClassTagHelperTests
    {
        private static readonly Guid Root = Guid.NewGuid();
        private static readonly Guid PageA = Guid.NewGuid();
        private static readonly Guid Child = Guid.NewGuid();

        private static readonly Dictionary<Guid, string> Urls = new()
        {
            [Root] = "https://example.com/en/",
            [PageA] = "https://example.com/en/page-a/",
            [Child] = "https://example.com/en/page-a/child/",
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

        private static async Task<TagHelperOutput> Render(Guid currentPage, string href, string? activeLink = null)
        {
            var content = Mock.Of<IPublishedContent>(c => c.Key == currentPage);
            var request = Mock.Of<IPublishedRequest>(r =>
                r.PublishedContent == content
                && r.Uri == new Uri("https://example.com/en/page-a/")
                && r.Culture == "en-US");
            var umbracoContext = Mock.Of<IUmbracoContext>(c => c.PublishedRequest == request);

            var umbracoContextAccessor = new Mock<IUmbracoContextAccessor>();
            umbracoContextAccessor.Setup(a => a.TryGetUmbracoContext(out umbracoContext)).Returns(true);

            IEnumerable<Guid> ancestorsOrSelf = currentPage == Child ? new[] { Child, PageA, Root } : new[] { PageA, Root };
            var navigation = new Mock<IDocumentNavigationQueryService>();
            navigation.Setup(n => n.TryGetAncestorsOrSelfKeys(currentPage, out ancestorsOrSelf)).Returns(true);

            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider
                .Setup(p => p.GetUrl(It.IsAny<Guid>(), UrlMode.Absolute, "en-US", It.IsAny<Uri?>()))
                .Returns((Guid key, UrlMode _, string? _, Uri? _) => Urls[key]);

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
