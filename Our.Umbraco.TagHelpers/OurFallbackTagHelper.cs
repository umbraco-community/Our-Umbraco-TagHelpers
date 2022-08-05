using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Fallback Umbraco properties
    /// </summary>
    [HtmlTargetElement("our-fallback")]
    public class OurFallbackTagHelper : TagHelper
    {
        /// <summary>
        /// The property on the Model to use as a fallback such as Model.Header
        /// Would just be Header
        /// </summary>
        [HtmlAttributeName("property")]
        public ModelExpression? ModelProperty { get; set; }

        [HtmlAttributeName("mode")]
        public Fallback Mode { get; set; }

        /// <summary>
        /// If using the fallback mode as Language then you can specify the culture to fallback to
        /// </summary>
        [HtmlAttributeName("culture")]
        public string? CultureCode { get; set; } = null;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext? ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Check to see that the Model Property is NOT a complex type or a collection of things
            var isComplexType = ModelProperty != null && ModelProperty.ModelExplorer.Metadata.IsComplexType;
            var isEnumerableType = ModelProperty != null && ModelProperty.ModelExplorer.Metadata.IsEnumerableType;

            // Only can support simpler things such as strings (as if collection)
            // We have no way of knowing how they want to iterate and display that HTML etc
            if(isComplexType || isEnumerableType)
            {
                output.SuppressOutput();
                return;
            }

            // Attempt to fetch/cast the Model as IPublishedContent
            var contentNode = ViewContext?.ViewData.Model as IPublishedContent;
            if(contentNode == null)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = ""; // Remove the outer <our-fallback>

            // Get the Model property with fallback mode
            var result = contentNode.Value(ModelProperty?.Name, fallback: Mode, culture: CultureCode);

            // If we have a value that can be
            if (string.IsNullOrWhiteSpace($"{result}") == false)
            {
                output.Content.SetHtmlContent($"{result}");
                return;
            }
        }
    }
}
