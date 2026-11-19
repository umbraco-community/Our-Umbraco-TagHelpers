using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Outputs the hreflang alternate links for the current page, for multilingual sites:
    /// <c>&lt;link rel="alternate" hreflang="da" href="https://example.dk/om-os/" /&gt;</c> for each culture the page
    /// is published in (including the current one), plus an <c>x-default</c> link to the default language's version.
    /// Use it in the &lt;head&gt;: <c>&lt;our-hreflang /&gt;</c>
    /// </summary>
    /// <remarks>
    /// The URLs are absolute and come from Umbraco's URL provider, so culture domains on one or more hosts are handled.
    /// Nothing is output for content that doesn't vary by culture, when the page is published in only one culture,
    /// or when the request isn't for an Umbraco page.
    /// </remarks>
    [HtmlTargetElement("our-hreflang", TagStructure = TagStructure.WithoutEndTag)]
    public class HreflangTagHelper : TagHelper
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly ILanguageService _languageService;

        public HreflangTagHelper(IUmbracoContextAccessor umbracoContextAccessor, IPublishedUrlProvider publishedUrlProvider, ILanguageService languageService)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _publishedUrlProvider = publishedUrlProvider;
            _languageService = languageService;
        }

        /// <summary>
        /// Which version the x-default link points to:
        /// <list type="bullet">
        /// <item>not set, empty or "true": the default language's version;</item>
        /// <item>a culture's ISO code as set up in Umbraco, such as "da" or "en-US": that culture's version;</item>
        /// <item>"false": no x-default link.</item>
        /// </list>
        /// Values ignore case. When the chosen culture isn't a language of the site, or the page isn't published in it,
        /// the x-default link is left out.
        /// </summary>
        [HtmlAttributeName("x-default")]
        public string? XDefault { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // never render <our-hreflang> itself

            var request = _umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) ? umbracoContext.PublishedRequest : null;
            var content = request?.PublishedContent;

            // ContentType is read through IPublishedElement, which declares it on every supported Umbraco version
            // (Umbraco 18 moved members from IPublishedContent to IPublishedElement, and the 17 build also runs on 18)
            if (request is null || content is null || !((IPublishedElement)content).ContentType.VariesByCulture())
            {
                return;
            }

            var languages = (await _languageService.GetAllAsync()).ToList();
            var links = new List<(string Culture, string Url)>();
            foreach (var language in languages)
            {
                // The URL provider returns "#" (or nothing) for a culture the page isn't published in
                var url = _publishedUrlProvider.GetUrl(((IPublishedElement)content).Key, UrlMode.Absolute, language.IsoCode, request.Uri);
                if (Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    links.Add((language.IsoCode, url));
                }
            }

            if (links.Count < 2)
            {
                return;
            }

            var html = new StringBuilder();
            foreach (var (culture, url) in links)
            {
                AppendLink(html, culture, url);
            }

            if (!string.Equals(XDefault?.Trim(), "false", StringComparison.OrdinalIgnoreCase))
            {
                // Not set or "true": the default language; otherwise the culture's ISO code
                var xDefaultCulture = string.IsNullOrWhiteSpace(XDefault) || string.Equals(XDefault.Trim(), "true", StringComparison.OrdinalIgnoreCase)
                    ? languages.FirstOrDefault(l => l.IsDefault)?.IsoCode
                    : XDefault.Trim();
                var xDefault = links.FirstOrDefault(l => string.Equals(l.Culture, xDefaultCulture, StringComparison.OrdinalIgnoreCase));
                if (xDefault.Url is not null)
                {
                    AppendLink(html, "x-default", xDefault.Url);
                }
            }

            output.Content.SetHtmlContent(html.ToString());
        }

        private static void AppendLink(StringBuilder html, string hreflang, string url) =>
            html.Append("<link rel=\"alternate\" hreflang=\"")
                .Append(HtmlEncoder.Default.Encode(hreflang))
                .Append("\" href=\"")
                .Append(HtmlEncoder.Default.Encode(url))
                .Append("\" />\n");
    }
}
