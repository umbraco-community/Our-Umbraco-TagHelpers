using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models;

namespace Our.Umbraco.TagHelpers
{
    [HtmlTargetElement("our-link")]
    public class LinkTagHelper : TagHelper
    {
        [HtmlAttributeName("Link")]
        public Link? Link { get; set; }

        /// <summary>
        /// If the link should render child content even if there is no link
        /// </summary>
        [HtmlAttributeName("Fallback")]
        public bool Fallback { get; set; }

        /// <summary>
        /// element to replace the anchor with when there is no link i.e div
        /// </summary>
        [HtmlAttributeName("FallbackElement")]
        public string? FallbackElement { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // If the <our-link /> is self closing
            // Ensure that our <a></a> always has a matching end tag
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "a";

            var childContent = await output.GetChildContentAsync();

            // Ensure we have a Url set on the LinkPicker property editor in Umbraco
            if (string.IsNullOrWhiteSpace(Link?.Url))
            {
                if (Fallback && !childContent.IsEmptyOrWhiteSpace)
                {
                    output.TagName = FallbackElement;
                }
                else
                {
                    output.SuppressOutput();
                }

                return;
            }

            // If we use the TagHelper <umb-link></umb-link>
            // Without child DOM elements then it will use the Link Name property inside the <a> it generates
            if (childContent.IsEmptyOrWhiteSpace)
            {
                output.Content.SetContent(Link.Name);
            }

            // Set the HREF of the <a>
            output.Attributes.SetAttribute("href", Link.Url);

            if (string.IsNullOrWhiteSpace(Link.Target))
            {
                return;
            }

            // Set the <a target=""> attribute such as _blank etc...
            output.Attributes.SetAttribute("target", Link.Target);

            // If the target is _blank & not an internal picked content node & external
            // Ensure we set the <a rel="noopener"> attribute
            if (Link.Target == "_blank" && Link.Type == LinkType.External)
            {
                output.Attributes.SetAttribute("rel", "noopener");
            }
        }
    }
}
