using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Utils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// This allows you to inline an SVG file into the DOM
    /// from a file on disk or an Umbraco Media Item
    /// </summary>
    [HtmlTargetElement("our-svg")]
    public class InlineSvgTagHelper : TagHelper
    {
        private MediaFileManager _mediaFileManager;
        private IWebHostEnvironment _webHostEnvironment;
        private IPublishedUrlProvider _urlProvider;
        private OurUmbracoTagHelpersConfiguration _globalSettings;
        private AppCaches _appCaches;

        public InlineSvgTagHelper(MediaFileManager mediaFileManager, IWebHostEnvironment webHostEnvironment, IPublishedUrlProvider urlProvider, IOptions<OurUmbracoTagHelpersConfiguration> globalSettings, AppCaches appCaches)
        {
            _mediaFileManager = mediaFileManager;
            _webHostEnvironment = webHostEnvironment;
            _urlProvider = urlProvider;
            _globalSettings = globalSettings.Value;
            _appCaches = appCaches;
        }

        /// <summary>
        /// A filepath to a SVG on disk such as /assets/icon.svg
        /// NOTE: You cannot use this in conjuction with the media-item attribute
        /// </summary>
        [HtmlAttributeName("src")]
        public string? FileSource { get; set; }

        /// <summary>
        /// An IPublishedContent Umbraco Media Item that has an .svg file extension
        /// NOTE: You cannot use this in conjuction with the src attribute
        /// </summary>
        [HtmlAttributeName("media-item")]
        public IPublishedContent? MediaItem { get; set; }

        /// <summary>
        /// A classic CSS class property to apply/append a CSS class or classes.
        /// </summary>
        [HtmlAttributeName("class")]
        public string? CssClass { get; set; }

        /// <summary>
        /// A boolean to ensure a viewbox is present within the SVG tag to ensure the vector is always responsive.
        /// NOTE: Use the appsettings configuration to apply this globally (e.g. "Our.Umbraco.TagHelpers": { "InlineSvgTagHelper": { "EnsureViewBox": true } } ).
        /// </summary>
        [HtmlAttributeName("ensure-viewbox")]
        public bool EnsureViewBox { get; set; }

        /// <summary>
        /// A boolean to cache the SVG contents rather than performing the operation on each page load.
        /// NOTE: Use the appsettings configuration to apply this globally (e.g. "Our.Umbraco.TagHelpers": { "InlineSvgTagHelper": { "Cache": true } } ).
        /// </summary>
        [HtmlAttributeName("cache")]
        public bool Cache { get; set; }

        /// <summary>
        /// An integer to set the cache minutes. Default: 180 minutes.
        /// NOTE: Use the appsettings configuration to apply this globally (e.g. "Our.Umbraco.TagHelpers": { "InlineSvgTagHelper": { "CacheMinutes": 180 } } ).
        /// </summary>
        [HtmlAttributeName("cache-minutes")]
        public int CacheMinutes { get; set; }

        /// <summary>
        /// A boolean to ignore the appsettings. 
        /// NOTE: Applies to 'ensure-viewbox' & 'cache' only
        /// </summary>
        [HtmlAttributeName("ignore-appsettings")]
        public bool IgnoreAppSettings { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Can only use media-item OR src
            // Can't use both properties together
            if(string.IsNullOrWhiteSpace(FileSource) == false && MediaItem is not null)
            {
                // KABOOM !
                // Can't decide which property to use to render inline SVG
                // So just render nothing...
                output.SuppressOutput();
                return;
            }

            string? cleanedFileContents = null;

            if(Cache || (_globalSettings.OurSVG.Cache && !IgnoreAppSettings))
            {
                var cacheName = string.Empty;
                var cacheMins = CacheMinutes > 0 ? CacheMinutes : _globalSettings.OurSVG.CacheMinutes;

                if (MediaItem is not null)
                {
                    cacheName = string.Concat("MediaItem-SvgContents (", MediaItem.Key.ToString(), ")");
                }
                else if (string.IsNullOrWhiteSpace(FileSource) == false)
                {
                    cacheName = string.Concat("File-SvgContents (", FileSource, ")");
                }

                cleanedFileContents = _appCaches.RuntimeCache.GetCacheItem(cacheName, () =>
                {
                    return GetFileContents();
                }, TimeSpan.FromMinutes(cacheMins));
            }
            else
            {
                cleanedFileContents = GetFileContents();
            }

            if (string.IsNullOrEmpty(cleanedFileContents))
            {
                output.SuppressOutput();
                return;
            }

            // Remove the src attribute or media-item from the <svg>
            output.Attributes.RemoveAll("src");
            output.Attributes.RemoveAll("media-item");

            output.TagName = null; // Remove <our-svg>
            output.Content.SetHtmlContent(cleanedFileContents);
       }

        private string? GetFileContents()
        {
            // SVG fileContents to render to DOM
            var fileContents = string.Empty;

            if (MediaItem is not null)
            {
                // Check Umbraco Media Item that is picked/used
                // has a file that uses a .svg file extension
                var mediaItemPath = MediaItem.Url(_urlProvider);
                if (mediaItemPath?.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) != true)
                {
                    return null;
                }

                // Ensure the file actually exists on disk, Azure blob provider or ...
                // Anywhere else defined by IFileSystem to fetch & store files
                if (_mediaFileManager.FileSystem.FileExists(mediaItemPath) == false)
                {
                    return null;
                }

                // Read its contents (get its stream)
                var fileStream = _mediaFileManager.FileSystem.OpenFile(mediaItemPath);
                using var reader = new StreamReader(fileStream);
                fileContents = reader.ReadToEnd();
            }
            else if (string.IsNullOrWhiteSpace(FileSource) == false)
            {
                // Check string src filepath ends with .svg
                if (FileSource.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    return null;
                }

                // Get file from wwwRoot using a path such as
                // /assets/logo/my-logo.svg as opposed to wwwRoot/assets/logo/my-logo.svg
                // Can we or should we support ~ in paths at root?
                var webRoot = _webHostEnvironment.WebRootFileProvider;
                var file = webRoot.GetFileInfo(FileSource);

                // Ensure file exists in wwwroot path
                if (file.Exists == false)
                {
                    return null;
                }

                using var reader = new StreamReader(file.CreateReadStream());
                fileContents = reader.ReadToEnd();
            }

            // Sanitize SVG: remove script blocks and script URLs, then everything else that can run script (RemoveUnsafeContent)
            // https://stackoverflow.com/questions/65247336/is-there-anyway-to-sanitize-svg-file-in-c-any-libraries-anything/65375485#65375485
            var cleanedFileContents = Regex.Replace(fileContents,
                @"<script.*?script>",
                @"",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            cleanedFileContents = Regex.Replace(cleanedFileContents,
                @"javascript:",
                @"syntax:error:",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            try
            {
                cleanedFileContents = RemoveUnsafeContent(cleanedFileContents);
            }
            catch (Exception)
            {
                // Fail closed: an SVG that can't be checked isn't inlined
                return null;
            }

            if ((EnsureViewBox || (_globalSettings.OurSVG.EnsureViewBox && !IgnoreAppSettings)) || !string.IsNullOrEmpty(CssClass))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(cleanedFileContents);
                var svgs = doc.DocumentNode.SelectNodes("//svg");
                foreach (var svgNode in svgs)
                {
                    if (!string.IsNullOrEmpty(CssClass))
                    {
                        svgNode.AddClass(CssClass);
                    }
                    if ((EnsureViewBox || (_globalSettings.OurSVG.EnsureViewBox && !IgnoreAppSettings)) && svgNode.Attributes.Contains("width") && svgNode.Attributes.Contains("height") && !svgNode.Attributes.Contains("viewbox"))
                    {
                        var width = StringUtils.GetDecimal(svgNode.GetAttributeValue("width", "0"));
                        var height = StringUtils.GetDecimal(svgNode.GetAttributeValue("height", "0"));
                        svgNode.SetAttributeValue("viewbox", $"0 0 {width} {height}");

                        svgNode.Attributes.Remove("width");
                        svgNode.Attributes.Remove("height");
                    }
                }
                cleanedFileContents = doc.DocumentNode.OuterHtml;
            }

            return cleanedFileContents;
        }

        // Elements that can run script or load another document when the SVG is inlined into the page
        private static readonly HashSet<string> UnsafeElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "script", "foreignObject", "iframe", "frame", "embed", "object", "applet", "base",
        };

        // Animation elements, which can change another attribute (e.g. turn a link into a script URL)
        private static readonly HashSet<string> AnimationElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "set", "animate", "animateMotion", "animateTransform",
        };

        private static readonly Regex UrlScheme = new("^([a-zA-Z][a-zA-Z0-9+.-]*):", RegexOptions.Compiled);

        /// <summary>
        /// Removes what can run script once the SVG is inlined into the page: script-capable elements, event
        /// handler attributes (on...), links that aren't internal (#...), web or image references, and
        /// animations that target links or event handlers. Returns the SVG unchanged when there's nothing to remove.
        /// </summary>
        private static string RemoveUnsafeContent(string svg)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(svg);
            var changed = false;

            foreach (var node in doc.DocumentNode.Descendants().Where(n => n.NodeType == HtmlNodeType.Element).ToList())
            {
                if (UnsafeElements.Contains(node.Name)
                    || (AnimationElements.Contains(node.Name) && IsUnsafeAttributeName(node.GetAttributeValue("attributeName", string.Empty))))
                {
                    node.Remove();
                    changed = true;
                    continue;
                }

                foreach (var attribute in node.Attributes.ToList())
                {
                    if (attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                        || (IsLinkAttribute(attribute.Name) && !IsSafeLink(node.Name, attribute.Value ?? string.Empty)))
                    {
                        attribute.Remove();
                        changed = true;
                    }
                }
            }

            return changed ? doc.DocumentNode.OuterHtml : svg;
        }

        private static bool IsUnsafeAttributeName(string name) =>
            name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || IsLinkAttribute(name);

        private static bool IsLinkAttribute(string name) =>
            name.Equals("href", StringComparison.OrdinalIgnoreCase) || name.EndsWith(":href", StringComparison.OrdinalIgnoreCase);

        private static bool IsSafeLink(string element, string value)
        {
            // Browsers ignore whitespace and control characters in URLs, and entities are decoded first
            var url = new string((HtmlEntity.DeEntitize(value) ?? string.Empty).Where(c => c > ' ').ToArray());
            if (url.Length == 0 || url.StartsWith('#'))
            {
                return true;
            }

            var scheme = UrlScheme.Match(url).Groups[1].Value.ToLowerInvariant();
            return element.ToLowerInvariant() switch
            {
                // Links: web, mail and relative URLs
                "a" => scheme is "" or "http" or "https" or "mailto",
                // Images: web and relative URLs, and data URLs of images
                "image" or "feimage" => scheme is "" or "http" or "https"
                    || (scheme == "data" && url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)),
                // Everything else (use, textPath, gradients...) may only reference the SVG's own elements
                _ => false,
            };
        }
    }
}
