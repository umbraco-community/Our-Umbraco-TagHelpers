#nullable enable
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/54
    // <h1 our-text-if="@Model.Heading" /> renders <h1>Heading</h1>, or nothing when there's no text
    public class TextIfTagHelperTests
    {
        private static TagHelperOutput Render(object? text, string childContent = "", TagMode tagMode = TagMode.SelfClosing)
        {
            var output = TestContextHelpers.GetTagHelperOutput("h1", childContent: childContent);
            output.TagMode = tagMode;
            new TextIfTagHelper { Text = text }.Process(TestContextHelpers.GetTagHelperContext(), output);
            return output;
        }

        [Test]
        public void Text_IsTheElementsContent()
        {
            var output = Render("My heading");

            ClassicAssert.AreEqual("h1", output.TagName);
            ClassicAssert.AreEqual(TagMode.StartTagAndEndTag, output.TagMode); // <h1 ... /> needs an end tag for its text
            ClassicAssert.AreEqual("My heading", output.Content.GetContent());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NoText_LeavesTheElementOut(string? text)
        {
            var output = Render(text);

            ClassicAssert.IsNull(output.TagName);
            ClassicAssert.IsTrue(output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void Text_ReplacesTheContentInsideTheElement()
        {
            var output = Render("My heading", childContent: "Default", tagMode: TagMode.StartTagAndEndTag);

            ClassicAssert.AreEqual("My heading", output.Content.GetContent());
        }

        [Test]
        public void NoText_LeavesTheContentInsideTheElementOutToo()
        {
            // our-text-if is for empty elements: content inside is never shown, not even as a fallback
            var output = Render(null, childContent: "Default", tagMode: TagMode.StartTagAndEndTag);
            ClassicAssert.IsNull(output.TagName);
            ClassicAssert.IsTrue(output.IsContentModified, "Razor would otherwise render the original content inside the element");
            ClassicAssert.IsTrue(output.Content.IsEmptyOrWhiteSpace);
        }

        [Test]
        public void Markup_InText_IsShownAsText()
        {
            ClassicAssert.AreEqual("&lt;em&gt;Not emphasised&lt;/em&gt;", Render("<em>Not emphasised</em>").Content.GetContent());
        }

        [Test]
        public void RichText_IsRenderedAsHtml()
        {
            ClassicAssert.AreEqual("<strong>Rich</strong> text", Render(new HtmlEncodedString("<strong>Rich</strong> text")).Content.GetContent());
        }

        [Test]
        public void HtmlContent_IsRenderedAsHtml()
        {
            ClassicAssert.AreEqual("<b>Html</b>", Render(new HtmlString("<b>Html</b>")).Content.GetContent());
        }

        [Test]
        public void EmptyRichText_LeavesTheElementOut()
        {
            ClassicAssert.IsNull(Render(new HtmlEncodedString(" ")).TagName);
        }

        [Test]
        public void OtherValues_AreShownAsText()
        {
            ClassicAssert.AreEqual("42", Render(42).Content.GetContent());
        }
    }
}
