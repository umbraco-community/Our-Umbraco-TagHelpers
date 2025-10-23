using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Models;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class LinkTagHelperTests
    {
        private static readonly Link _onSiteLink = new() { Url = "/", Name = "example" };

        private static readonly Link _externalLink = new() { Url = "/", Name = "example", Target = "_blank", Type = LinkType.External };

        private TagHelperContext _tagHelperContext;

        [SetUp]
        public void SetUp()
        {
            _tagHelperContext = TestContextHelpers.GetTagHelperContext("link");
        }

        [TestCase("", "example")]
        [TestCase(null, "example")]
        [TestCase("content", "content")]
        public async Task Internal_Link_Renders_AnchorAroundContentOrLinkName(string childContent, string expectedContent)
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: childContent);
            output.Content.SetContent(childContent);

            LinkTagHelper tagHelper = new() { Link = _onSiteLink };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual($"<a href=\"/\">{expectedContent}</a>", markup);
        }

        [TestCase("", "example")]
        [TestCase(null, "example")]
        [TestCase("content", "content")]
        public async Task External_Link_Renders_AnchorAroundContentOrLinkName(string childContent, string expectedContent)
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: childContent);
            output.Content.SetContent(childContent);

            LinkTagHelper tagHelper = new() { Link = _externalLink };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual($"<a href=\"/\" target=\"_blank\" rel=\"noopener\">{expectedContent}</a>", markup);
        }

        [Test]
        public async Task NoUrl_WithoutFallback_RendersNothing()
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: string.Empty);
            output.Content.SetContent(string.Empty);

            LinkTagHelper tagHelper = new() { Link = new() };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual(string.Empty, markup);
        }

        [Test]
        public async Task Null_Link_WithoutFallback_RendersNothing()
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: string.Empty);
            output.Content.SetContent(string.Empty);

            LinkTagHelper tagHelper = new() { Link = null };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual(string.Empty, markup);
        }

        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("content", "content")]
        public async Task Null_Link_WithFallback_NoElement_RendersContent(string childContent, string expectedContent)
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: childContent);
            output.Content.SetContent(childContent);

            LinkTagHelper tagHelper = new() { Link = null, Fallback = true };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual(expectedContent, markup);
        }

        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("content", "<div>content</div>")]
        public async Task Null_Link_WithFallback_AndElement_RendersContent(string childContent, string expectedContent)
        {
            var output = TestContextHelpers.GetTagHelperOutput("our-link", childContent: childContent);
            output.Content.SetContent(childContent);

            LinkTagHelper tagHelper = new() { Link = null, Fallback = true, FallbackElement = "div" };

            var markup = await GetMarkupAsync(tagHelper, output);
            ClassicAssert.AreEqual(expectedContent, markup);
        }

        private async Task<string> GetMarkupAsync(LinkTagHelper tagHelper, TagHelperOutput output)
        {
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            using var txtWriter = new StringWriter();
            output.WriteTo(txtWriter, HtmlEncoder.Default);
            return txtWriter.ToString();
        }
    }
}
