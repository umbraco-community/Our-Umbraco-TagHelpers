using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Notifications;
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
        private readonly ILogger<InlineSvgTagHelper> _logger;

        public InlineSvgTagHelper(MediaFileManager mediaFileManager, IWebHostEnvironment webHostEnvironment, IPublishedUrlProvider urlProvider, IOptions<OurUmbracoTagHelpersConfiguration> globalSettings, AppCaches appCaches, ILogger<InlineSvgTagHelper> logger)
        {
            _mediaFileManager = mediaFileManager;
            _webHostEnvironment = webHostEnvironment;
            _urlProvider = urlProvider;
            _globalSettings = globalSettings.Value;
            _appCaches = appCaches;
            _logger = logger;
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
        /// Whether to cache the SVG contents rather than reading the file on each page load.
        /// When it isn't set, the appsettings value is used (unless 'ignore-appsettings' is set), so cache="false"
        /// turns caching off for this tag even when it's on globally.
        /// NOTE: Use the appsettings configuration to apply this globally (e.g. "Our.Umbraco.TagHelpers": { "OurSVG": { "Cache": true } } ).
        /// </summary>
        [HtmlAttributeName("cache")]
        public bool Cache
        {
            get => _cache ?? false;
            set => _cache = value;
        }

        // Razor only sets Cache when the tag has a cache attribute, so null means "not set": use appsettings.
        // Kept private so Cache stays a bool, as in earlier versions.
        private bool? _cache;

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
        /// <summary>
        /// Gives every id in the SVG a prefix of its own for this tag, and updates the references to them (url(#...),
        /// href="#...", aria-labelledby and #id selectors in &lt;style&gt;), so the same SVG twice on a page, or two SVGs
        /// using the same ids, don't share gradients, clip paths or filters.
        /// When it isn't set, the appsettings value is used (unless 'ignore-appsettings' is set).
        /// NOTE: Use the appsettings configuration to apply this globally (e.g. "Our.Umbraco.TagHelpers": { "OurSVG": { "UniqueIds": true } } ).
        /// </summary>
        [HtmlAttributeName("unique-ids")]
        public bool UniqueIds
        {
            get => _uniqueIds ?? false;
            set => _uniqueIds = value;
        }

        // Like _cache: null means the tag has no unique-ids attribute, so appsettings is used
        private bool? _uniqueIds;

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

            // The tag's own cache attribute wins; otherwise use appsettings (#57)
            if (_cache ?? (_globalSettings.OurSVG.Cache && !IgnoreAppSettings))
            {
                var cacheName = string.Empty;
                var cacheMins = CacheMinutes > 0 ? CacheMinutes : _globalSettings.OurSVG.CacheMinutes;

                if (MediaItem is not null)
                {
                    cacheName = string.Concat(InlineSvgTagHelperNotifications.MediaCacheKeyPrefix, " (", MediaItem.Key.ToString(), ")");
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

            // After the cache, so tags sharing a cached file each get their own class and viewBox
            cleanedFileContents = ApplyTagAttributes(cleanedFileContents);
            if (string.IsNullOrEmpty(cleanedFileContents))
            {
                output.SuppressOutput();
                return;
            }

            // After the cache, so each tag gets its own ids even when they share a cached file (#115). Random rather than
            // a page counter, so an SVG inside <our-cache> can't get the same ids as one rendered on a later request.
            if (_uniqueIds ?? (_globalSettings.OurSVG.UniqueIds && !IgnoreAppSettings))
            {
                cleanedFileContents = MakeIdsUnique(cleanedFileContents, "s" + Guid.NewGuid().ToString("N")[..8] + "-");
            }

            // Remove the src attribute or media-item from the <svg>
            output.Attributes.RemoveAll("src");
            output.Attributes.RemoveAll("media-item");

            // Any other attributes on <our-svg> (aria-*, role, id, data-*, width...) go onto the <svg>, as the
            // <our-svg> element itself isn't rendered. Razor has already removed the tag helper's own attributes.
            cleanedFileContents = CopyAttributesToSvg(cleanedFileContents, output.Attributes);
            output.Attributes.Clear();

            output.TagName = null; // Remove <our-svg>
            output.Content.SetHtmlContent(cleanedFileContents);
       }

        /// <summary>
        /// Sets the given attributes on the root &lt;svg&gt;, replacing the file's own values, with their values
        /// HTML-encoded. Returns the SVG unchanged when there are no attributes.
        /// </summary>
        private static string CopyAttributesToSvg(string svg, TagHelperAttributeList attributes)
        {
            if (attributes.Count == 0)
            {
                return svg;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(svg);
            var root = doc.DocumentNode.SelectSingleNode("//svg");
            if (root is null)
            {
                return svg;
            }

            foreach (var attribute in attributes)
            {
                var value = attribute.ValueStyle == HtmlAttributeValueStyle.Minimized ? string.Empty : EncodeAttributeValue(attribute.Value);
                root.Attributes.Remove(attribute.Name);
                // HtmlAgilityPack writes attribute values as given, so the value is already HTML-encoded here
                root.Attributes.Add(doc.CreateAttribute(attribute.Name, value));
            }

            return doc.DocumentNode.OuterHtml;
        }

        // ARIA attributes whose value is a list of ids
        private static readonly HashSet<string> AriaIdReferences = new(StringComparer.OrdinalIgnoreCase)
        {
            "aria-labelledby", "aria-describedby", "aria-controls", "aria-owns", "aria-flowto",
            "aria-activedescendant", "aria-details", "aria-errormessage",
        };

        // url(#id), url('#id'), url("#id") and url(&quot;#id&quot;)
        private static readonly Regex UrlIdReference = new("(?<before>url\\(\\s*(?:['\"]|&quot;)?)#(?<id>[^)'\"&\\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // #id in CSS selectors
        private static readonly Regex CssIdSelector = new("#(?<id>-?[_a-zA-Z][\\w-]*)", RegexOptions.Compiled);

        /// <summary>
        /// Prefixes every id in the SVG, and the references to those ids: url(#...) in any attribute and in &lt;style&gt;,
        /// href and xlink:href values of "#id", ARIA id references and #id selectors in &lt;style&gt;. Only ids the SVG
        /// defines are changed, so colours such as "#fff" and references to other ids stay as they are.
        /// </summary>
        internal static string MakeIdsUnique(string svg, string prefix)
        {
            if (svg.IndexOf("id=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return svg;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(svg);
            var elements = doc.DocumentNode.Descendants().Where(node => node.NodeType == HtmlNodeType.Element).ToList();
            var ids = new HashSet<string>(
                elements.Select(node => node.GetAttributeValue("id", string.Empty)).Where(id => id.Length > 0),
                StringComparer.Ordinal);
            if (ids.Count == 0)
            {
                return svg;
            }

            string RenameUrls(string text) => UrlIdReference.Replace(text, match =>
                ids.Contains(match.Groups["id"].Value) ? match.Groups["before"].Value + "#" + prefix + match.Groups["id"].Value : match.Value);

            foreach (var element in elements)
            {
                foreach (var attribute in element.Attributes)
                {
                    var value = attribute.Value ?? string.Empty;
                    if (attribute.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = prefix + value;
                    }
                    else if ((attribute.Name.Equals("href", StringComparison.OrdinalIgnoreCase) || attribute.Name.Equals("xlink:href", StringComparison.OrdinalIgnoreCase))
                        && value.StartsWith('#') && ids.Contains(value[1..]))
                    {
                        attribute.Value = "#" + prefix + value[1..];
                    }
                    else if (AriaIdReferences.Contains(attribute.Name))
                    {
                        attribute.Value = string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(id => ids.Contains(id) ? prefix + id : id));
                    }
                    else if (value.Contains("url(", StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = RenameUrls(value);
                    }
                }

                if (element.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
                {
                    // url(#id) first; the renamed ids are no longer in the set, so the selector pass leaves them alone
                    var css = RenameUrls(element.InnerHtml);
                    element.InnerHtml = CssIdSelector.Replace(css, match => ids.Contains(match.Groups["id"].Value) ? "#" + prefix + match.Groups["id"].Value : match.Value);
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        private static string EncodeAttributeValue(object? value)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case IHtmlContent html:
                    // e.g. aria-label="@Model.Name": Razor has already encoded it
                    using (var writer = new StringWriter())
                    {
                        html.WriteTo(writer, HtmlEncoder.Default);
                        return writer.ToString();
                    }
                default:
                    return HtmlEncoder.Default.Encode(value.ToString() ?? string.Empty);
            }
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

            // Nothing to render (no media item or src, or an empty file) - bail out before parsing
            if (string.IsNullOrWhiteSpace(fileContents))
            {
                return null;
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
            catch (Exception exc)
            {
                // Fail closed: an SVG that can't be checked isn't inlined
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(exc, "Invalid svg markup");
                }
                return string.Empty;
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

        /// <summary>
        /// Applies this tag's class and viewBox to the sanitized SVG. This runs for every tag, after the cache:
        /// the cache holds one file's contents, shared by every tag that uses the file.
        /// </summary>
        private string ApplyTagAttributes(string cleanedFileContents)
        {
            if ((EnsureViewBox || (_globalSettings.OurSVG.EnsureViewBox && !IgnoreAppSettings)) || !string.IsNullOrEmpty(CssClass))
            {
                HtmlDocument doc = new HtmlDocument();
                try {
                    doc.LoadHtml(cleanedFileContents);
                    // SelectNodes returns null rather than an empty collection when nothing matches
                    var svgs = doc.DocumentNode.SelectNodes("//svg");
                    if (svgs is null)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning("Invalid svg markup: no <svg> element found");
                        }
                        return string.Empty;
                    }

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
                catch(Exception exc) {
                    if(_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(exc, "Invalid svg markup");
                    }
                    return string.Empty;
                }
            }

            return cleanedFileContents;
        }
    }
}
