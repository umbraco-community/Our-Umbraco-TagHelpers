using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// An attribute TagHelper that only works with <a> tags
    /// 
    /// Applying the attribute our-is-active-page="navi-active"
    /// Will apply the CSS class name navi-active to the class attribute
    /// if the value in the href of the <a> compared to the page being rendered
    /// is the current page or part of an ancestor
    /// </summary>"
    [HtmlTargetElement("*", Attributes = tagHelperAttributes)]
    [HtmlTargetElement("a", Attributes = tagHelperAttributeName)]
    public class ActiveClassTagHelper : TagHelper
    {
        private const string tagHelperAttributeName = "our-active-class";
        private const string tagHelperAttributeHrefName = "our-active-href";
        private const string tagHelperAttributes = tagHelperAttributeName + ", " + tagHelperAttributeHrefName;

        private IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IDocumentUrlService _documentUrlService;

        public ActiveClassTagHelper(IUmbracoContextAccessor umbracoContextAccessor, IDocumentUrlService documentUrlService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _documentUrlService = documentUrlService;
        }

        /// <summary>
        /// The CSS class name you wish to append to the class attribute
        /// on an <a> tag when the page is active/selected
        /// </summary>
        [HtmlAttributeName(tagHelperAttributeName)]
        public string? ActiveClassName { get; set; }


        /// <summary>
        /// If you wish to add an active CSS class on another DOM element than an <a>
        /// You can use this attribute to pass a link to the page to check in conjuction with `our-active-class` attribute
        /// </summary>
        [HtmlAttributeName("our-active-href")]
        public string? ActiveLink { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Remove the attribute 
            // We don't want it in the markup we send down to the page
            output.Attributes.RemoveAll(tagHelperAttributeName);

            var ctx = _umbracoContextAccessor.GetRequiredUmbracoContext();

            // If we have active link prop set use that othewise try to find the href attribute on an <a> and its value
            var href = string.IsNullOrEmpty(ActiveLink) ? output.Attributes["href"]?.Value.ToString() : ActiveLink;
            if (string.IsNullOrEmpty(href))
            {
                return;
            }

            // Try & parse href as URI, as it could be relative or absolute
            // or contain a quersystring we only want the path part
            if (Uri.TryCreate(href, UriKind.Absolute, out Uri? link) || Uri.TryCreate(ctx.PublishedRequest.Uri, href, out link))
            {
                // Get the node based of the value in the HREF
                // GetByRoute on IPublishedContentCache is obsolete now - need to use DocumentUrlService instead
                var documentKeyFromUrl = _documentUrlService.GetDocumentKeyByRoute(link.AbsolutePath, null, null, false);
                var nodeOfLink = documentKeyFromUrl is not null ? ctx.Content.GetById(documentKeyFromUrl.Value) : null;
                
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
