using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// This allows you to set some PageSpeed Insights friendly optimisations to your img tags
    /// including lazy loading placeholders, dynamic alt text, aspect ratios 
    /// & image quality (for non-media items, but still on the file system)
    /// </summary>
    [HtmlTargetElement("our-img")]
    public class ImgTagHelper : TagHelper
    {
        private MediaFileManager _mediaFileManager;
        private IWebHostEnvironment _webHostEnvironment;
        private OurUmbracoTagHelpersConfiguration _globalSettings;
        private AppCaches _appCaches;
        private IMediaService _mediaService;
        private UmbracoHelper _umbracoHelper;

        public ImgTagHelper(MediaFileManager mediaFileManager, IWebHostEnvironment webHostEnvironment, IOptions<OurUmbracoTagHelpersConfiguration> globalSettings, AppCaches appCaches, IMediaService mediaService, UmbracoHelper umbracoHelper)
        {
            _mediaFileManager = mediaFileManager;
            _webHostEnvironment = webHostEnvironment;
            _globalSettings = globalSettings.Value;
            _appCaches = appCaches;
            _mediaService = mediaService;
            _umbracoHelper = umbracoHelper;
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
        public MediaWithCrops? MediaItem { get; set; }

        [HtmlAttributeName("alt")]
        public string? ImgAlt { get; set; }

        [HtmlAttributeName("width")]
        public int ImgWidth { get; set; }

        [HtmlAttributeName("height")]
        public int ImgHeight { get; set; }

        [HtmlAttributeName("cropalias")]
        public string ImgCropAlias { get; set; }

        [HtmlAttributeName("style")]
        public string? ImgStyle { get; set; }

        [HtmlAttributeName("quality")]
        public int ImgQuality { get; set; }

        [HtmlAttributeName("class")]
        public string? ImgClass { get; set; }

        [HtmlAttributeName("sizes")]
        public List<OurImageSize> ImgSizes { get; set; }

        [HtmlAttributeName("abovethefold")]
        public bool AboveTheFold { get; set; }

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
            if (string.IsNullOrWhiteSpace(FileSource) == false && MediaItem is not null)
            {
                // KABOOM !
                // Can't decide which property to use to render inline SVG
                // So just render nothing...
                output.SuppressOutput();
                return;
            }
            var width = 0d;
            var height = 0d;
            var cssClasses = new List<string>();
            var imgSrc = string.Empty;
            var placeholderImgSrc = string.Empty;
            var jsLazyLoad = !_globalSettings.ImgTagHelper.UseNativeLazyLoading && !AboveTheFold;
            IMedia media = default;

            if (!string.IsNullOrEmpty(ImgClass))
            {
                cssClasses.Add(ImgClass);
            }

            if (MediaItem is not null)
            {
                media = _mediaService.GetById(MediaItem.Id);
                width = ImgWidth;
                if (!string.IsNullOrEmpty(ImgCropAlias))
                {
                    imgSrc = MediaItem.GetCropUrl(width: (int)width, cropAlias: ImgCropAlias);
                    placeholderImgSrc = MediaItem.GetCropUrl(width: ImgWidth, cropAlias: ImgCropAlias, quality: 5);
                    var cropWidth = MediaItem.LocalCrops.GetCrop(ImgCropAlias).Width;
                    var cropHeight = MediaItem.LocalCrops.GetCrop(ImgCropAlias).Height;
                    height = (cropHeight / cropWidth) * width;
                }
                else
                {
                    imgSrc = MediaItem.GetCropUrl(width: (int)width);
                    placeholderImgSrc = MediaItem.GetCropUrl(width: (int)width, quality: _globalSettings.ImgTagHelper.LazyLoadPlaceholderLowQualityImageQuality);
                    var originalWidth = media.GetValue<double>("umbracoWidth");
                    var originalHeight = media.GetValue<double>("umbracoHeight");
                    height = (originalHeight / originalWidth) * width;
                }
            }
            else if (!string.IsNullOrEmpty(FileSource))
            {
                width = ImgWidth;
                height = ImgHeight;
                imgSrc = FileSource;

                #region Autogenerate alt text
                if (string.IsNullOrWhiteSpace(ImgAlt))
                {
                    if (!string.IsNullOrEmpty(FileSource))
                    {
                        output.Attributes.Add("alt", GetImageAltText(FileSource));
                    }
                }
                #endregion

                #region If width & height are not defined then return a basic <img> with just a src, alt & class (if provided)
                if (ImgWidth == 0 || ImgHeight == 0)
                {
                    output.Attributes.Add("src", FileSource);

                    if (cssClasses.Count > 0)
                    {
                        output.Attributes.Add("class", string.Join(" ", cssClasses));
                    }

                    output.TagName = "img";
                    return;
                }
                #endregion

            }






            
            var style = ImgStyle;

            #region If width & height defined, add the aspect-ratio CSS property alongside the width & height attributes. 
            /// Having width & height by themselves forces the image to initially load as that size during page load until a stylesheet kicks in.
            /// PageSpeed Insights requires all images to have a width & height.
            /// aspect-ratio sizes the element consistently, so the ratio of an element stays the same as it grows or shrinks.
            if (width > 0 && height > 0 && _globalSettings.ImgTagHelper.ApplyAspectRatio)
            {
                var aspectRatio = $"aspect-ratio: {width} / {height};";
                style = style?.Trim().TrimEnd(';');
                if (!string.IsNullOrEmpty(ImgStyle))
                {
                    style += ";";
                    output.Attributes.RemoveAll("style");
                }
                style += aspectRatio;
                style += "width: 100%;";
                output.Attributes.Add("style", style);
            }

            if (width > 0 && height > 0)
            {
                output.Attributes.Add("width", width);
                output.Attributes.Add("height", height);
            }
            #endregion


            var quality = ImgQuality > 0 ? ImgQuality : 100;

            if (jsLazyLoad && width > 0)
            {
                output.Attributes.Add("data-src", imgSrc);
                if (_globalSettings.ImgTagHelper.LazyLoadPlaceholder.Equals(ImagePlaceholderType.LowQualityImage) && !string.IsNullOrEmpty(placeholderImgSrc))
                {
                    output.Attributes.Add("src", placeholderImgSrc);
                }
                else if(height > 0)
                {
                    output.Attributes.Add("src", $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'%3E%3C/svg%3E");
                }
                cssClasses.Add(_globalSettings.ImgTagHelper.LazyLoadCssClass);
            }
            else
            {
                output.Attributes.Add("src", imgSrc);
            }

            if (_globalSettings.ImgTagHelper.UseNativeLazyLoading)
            {

                if (AboveTheFold)
                {
                    output.Attributes.Add("loading", "eager"); // Load the image as soon as the page loads.
                    output.Attributes.Add("fetchpriority", "high"); // Prioritise the visible image on initial page load.
                }
                else
                {
                    output.Attributes.Add("loading", "lazy"); // Native browser lazy loading, deferring the loading until it reaches a certain distance from the viewport.
                    output.Attributes.Add("decoding", "async"); // Non-blocking image decoding, allowing your page content to display faster.
                    output.Attributes.Add("fetchpriority", "low"); // Downgrades the priority of the image given it's not currently visible on initial page load.
                }
            }

            if (cssClasses.Count > 0)
            {
                output.Attributes.Add("class", string.Join(" ", cssClasses));
            }

            output.TagName = "img";

            if(ImgSizes != null && ImgSizes.Any() && MediaItem != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<picture>");

                ImgSizes = _globalSettings.ImgTagHelper.MobileFirst ? ImgSizes.OrderByDescending(o => o.ScreenSize).ToList() : ImgSizes.OrderBy(o => o.ScreenSize).ToList();
                foreach (var size in ImgSizes)
                {
                    var minWidth = size.ScreenSize switch
                    {
                        OurScreenSize.ExtraExtraLarge => _globalSettings.ImgTagHelper.MediaQueries.ExtraExtraLarge,
                        OurScreenSize.ExtraLarge => _globalSettings.ImgTagHelper.MediaQueries.ExtraLarge,
                        OurScreenSize.Large => _globalSettings.ImgTagHelper.MediaQueries.Large,
                        OurScreenSize.Medium => _globalSettings.ImgTagHelper.MediaQueries.Medium,
                        OurScreenSize.Small => _globalSettings.ImgTagHelper.MediaQueries.Small,
                        _ => 0
                    };
                    var cropAlias = !string.IsNullOrEmpty(size.CropAlias) ?
                        size.CropAlias :
                        !string.IsNullOrEmpty(ImgCropAlias) ?
                            ImgCropAlias :
                            null;

                    sb.AppendLine($"<source {(jsLazyLoad ? "data-" : "")}srcset=\"{MediaItem.GetCropUrl(width: size.ImageWidth, cropAlias: cropAlias)}\" media=\"({(_globalSettings.ImgTagHelper.MobileFirst ? $"min-width: {minWidth}" : $"max-width: {minWidth-1}")}px)\" />");
                }
                output.PreElement.SetHtmlContent(sb.ToString());
                output.PostElement.SetHtmlContent("</picture>");
            }

        }

        public static string GetImageAltText(string url)
        {
            try
            {
                var baseUri = new Uri("http://doesntmatter");
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    uri = new Uri(baseUri, url);

                return Path.GetFileNameWithoutExtension(uri.LocalPath);

            }
            catch (Exception)
            {

            }

            return "";
        }
    }
}
