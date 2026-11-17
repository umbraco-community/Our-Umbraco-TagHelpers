using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Apply the attribute our-text-if to any element to fill it with that text, or leave the element out of the
    /// page when there's no text. <c>&lt;h1 our-text-if="@Model.Heading" /&gt;</c> renders <c>&lt;h1&gt;Heading&lt;/h1&gt;</c>,
    /// and nothing at all when the heading is empty.
    /// </summary>
    /// <remarks>
    /// Text is HTML-encoded, as @Model.Heading is in Razor; values that are already HTML (IHtmlEncodedString such as
    /// rich text, or IHtmlContent) are output as HTML. The text replaces any content inside the element.
    /// </remarks>
    [HtmlTargetElement("*", Attributes = "our-text-if")]
    public class TextIfTagHelper : TagHelper
    {
        /// <summary>
        /// The text to show. When it's null, empty or whitespace, the element isn't rendered.
        /// </summary>
        [HtmlAttributeName("our-text-if")]
        public object? Text { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            switch (Text)
            {
                case IHtmlEncodedString html when !string.IsNullOrWhiteSpace(html.ToHtmlString()):
                    output.Content.SetHtmlContent(html.ToHtmlString());
                    break;
                case IHtmlContent html and not IHtmlEncodedString:
                    output.Content.SetHtmlContent(html);
                    break;
                case not null and not IHtmlEncodedString when !string.IsNullOrWhiteSpace(Text.ToString()):
                    output.Content.SetContent(Text.ToString());
                    break;
                default:
                    output.SuppressOutput();
                    return;
            }

            // <h1 our-text-if="..." /> is written self-closing, but needs an end tag to hold the text
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}
