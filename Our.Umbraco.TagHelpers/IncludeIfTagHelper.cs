using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Apply the attribute our-if to any HTML DOM element for it to be included in the page
    /// as long as the value inside the attribute evaluates to true
    /// </summary>
    [HtmlTargetElement("*", Attributes = "our-if")]
    public class IncludeIfTagHelper : TagHelper
    {
        /// <summary>
        /// A boolean expression
        /// If it evaluates to true then it will be kept in the page
        /// </summary>
        [HtmlAttributeName("our-if")]
        public bool? Predicate { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!Predicate.HasValue || !Predicate.Value)
            {
                output.SuppressOutput();
            }
        }
    }
}
