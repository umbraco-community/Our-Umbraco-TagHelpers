using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System;

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
    [HtmlTargetElement("a", Attributes = tagHelperAttributeName)]
    public class IsActivePageTagHelper : TagHelper
    {
        private const string tagHelperAttributeName = "our-active-class";
        private IUmbracoContextAccessor _umbracoContextAccessor;

        public IsActivePageTagHelper(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        /// <summary>
        /// The CSS class name you wish to append to the class attribute
        /// on an <a> tag when the page is active/selected
        /// </summary>
        [HtmlAttributeName(tagHelperAttributeName)]
        public string ActiveClassName { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Remove the attribute 
            // We don't want it in the markup we send down to the page
            output.Attributes.RemoveAll(tagHelperAttributeName);

            var ctx = _umbracoContextAccessor.GetRequiredUmbracoContext();

            // On the <a> try to find the href attribute and its value
            var href = output.Attributes["href"]?.Value.ToString();
            if (string.IsNullOrEmpty(href))
            {
                return;
            }

            // Try & parse href as URI, as it could be relative or absolute
            // or contain a quersystring we only want the path part
            if (Uri.TryCreate(href, UriKind.Absolute, out Uri link) || Uri.TryCreate(ctx.PublishedRequest.Uri, href, out link))
            {
                // Get the node based of the value in the HREF
                var nodeOfLink = ctx.Content.GetByRoute(link.AbsolutePath);
                if (nodeOfLink == null)
                {
                    return;
                }

                // Get the current node of the page that is rendering
                var currentPageRendering = ctx.PublishedRequest.PublishedContent;

                // Check if thelink we are rendering is current page or an ancestor
                if (nodeOfLink.IsAncestorOrSelf(currentPageRendering))
                {
                    // Is active page
                    output.AddClass(ActiveClassName, HtmlEncoder.Default);
                }
            }
        }
    }
}
