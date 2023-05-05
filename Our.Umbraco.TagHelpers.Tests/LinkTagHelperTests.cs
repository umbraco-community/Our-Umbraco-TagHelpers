using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Models;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class LinkTagHelperTests
    {

        [TestCaseSource(nameof(TestCases))]
        public async Task LinkHelperTests(LinkTagHelper tagHelper, string childContent, string expectedOutput)
        {
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("link");
            var output = TestContextHelpers.GetTagHelperOutput("our-link",
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            output.Content.SetContent(childContent);

            await tagHelper.ProcessAsync(tagHelperContext, output);

            using var txtWriter = new StringWriter();
            output.WriteTo(txtWriter, HtmlEncoder.Default);
            Assert.AreEqual(expectedOutput, txtWriter.ToString());
        }

        private static IEnumerable<TestCaseData> TestCases()
        {
            var onSiteLink = new Link { Url = "/", Name = "example" };
            var externalLink = new Link { Url = "/", Name = "example", Target = "_blank", Type = LinkType.External };

            return new[] {
                new TestCaseData(new LinkTagHelper(), "content", string.Empty).SetName("No content rendered if Link is null"),

                new TestCaseData(new LinkTagHelper { Link = onSiteLink},null,"<a href=\"/\">example</a>").SetName("Internal link is rendered with no content"),
                new TestCaseData(new LinkTagHelper { Link = onSiteLink},"content","<a href=\"/\">content</a>").SetName("Internal link is rendered with content"),

                new TestCaseData(new LinkTagHelper { Link = externalLink},null,"<a href=\"/\" target=\"_blank\" rel=\"noopener\">example</a>").SetName("External link with target is rendered with no content"),
                new TestCaseData(new LinkTagHelper { Link = externalLink},"content","<a href=\"/\" target=\"_blank\" rel=\"noopener\">content</a>").SetName("External link with target is rendered with content"),


                new TestCaseData(new LinkTagHelper() { Fallback = true }, "", "").SetName("Fallback with no content"),
                new TestCaseData(new LinkTagHelper() { Fallback = true }, "content", "content").SetName("Fallback with only content"),
                new TestCaseData(new LinkTagHelper() { Fallback = true, FallbackElement = "div" }, "content", "<div>content</div>")
                .SetName("Fallback with fallback element and content")
            };
        }
    }
}
