using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// An attribute TagHelper that only works with <a> tags
    /// 
    /// Applying the attribute our-is-active-page="navi-active"
    /// Will apply the CSS class name navi-active to the class attribute
    /// if the value in the href of the <a> compared to the page being rendered
    /// is the current page or part of an ancestor
    /// </summary>
    [HtmlTargetElement("a", Attributes = "our-is-active-page")]
    public class IsActivePageTagHelper : TagHelper
    {
        private IUmbracoContextAccessor _umbracoContextAccessor;

        public IsActivePageTagHelper(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        /// <summary>
        /// The CSS class name you wish to append to the class attribute
        /// on an <a> tag when the page is active/selected
        /// </summary>
        [HtmlAttributeName("our-is-active-page")]
        public string ActiveClassName { get; set; }
        
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Remove the attribute 
            // We don't want it in the markup we send down to the page
            output.Attributes.RemoveAll("our-is-active-page");

            var ctx = _umbracoContextAccessor.GetRequiredUmbracoContext();

            // On the <a> try to find the href attribute and its value
            var href = output.Attributes["href"]?.Value.ToString();
            if (string.IsNullOrEmpty(href))
            {
                return;
            }

            // Clean href values of any querystrings
            // As we won't be able to query GetByRoute for an Umbraco node with them
            href = href.Substring(0, href.IndexOf("?") > 0 ? href.IndexOf("?") : href.Length);

            // Get the node based of the value in the HREF
            var nodeOfLink = ctx.Content.GetByRoute(href);
            if(nodeOfLink == null)
            {
                return;
            }

            // Get the current node of the page that is rendering
            var currentPageRendering = ctx.PublishedRequest.PublishedContent;

            // Check if thelink we are rendering is current page or an ancestor
            if(nodeOfLink.IsAncestorOrSelf(currentPageRendering))
            {
                // Is active page
                output.Attributes.AddClass(ActiveClassName, HtmlEncoder.Default);
            }
        }
    }
}
