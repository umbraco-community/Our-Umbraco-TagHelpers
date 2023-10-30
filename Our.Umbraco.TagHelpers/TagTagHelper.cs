using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    [HtmlTargetElement("*", Attributes = "our-tag")]
    public class TagTagHelper : TagHelper
    {
        public string? OurTag { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (OurTag?.IsNullOrWhiteSpace() == false)
            {
                output.TagName = OurTag.ToLower();
            }
        }
    }
}
