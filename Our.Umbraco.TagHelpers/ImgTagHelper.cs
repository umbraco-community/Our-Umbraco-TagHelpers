using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Classes;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Enums;
using Our.Umbraco.TagHelpers.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
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
        private OurUmbracoTagHelpersConfiguration _globalSettings;
        private IMediaService _mediaService;

        public ImgTagHelper(IOptions<OurUmbracoTagHelpersConfiguration> globalSettings, IMediaService mediaService)
        {
            _globalSettings = globalSettings.Value;
            _mediaService = mediaService;
        }

        /// <summary>
        /// A filepath to an image on disk such as /assets/image.jpg, external URL's can also be used with limited functionality
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

        [HtmlAttributeName("width--s")]
        public int ImgWidthSmall { get; set; }

        [HtmlAttributeName("width--m")]
        public int ImgWidthMedium { get; set; }

        [HtmlAttributeName("width--l")]
        public int ImgWidthLarge { get; set; }

        [HtmlAttributeName("width--xl")]
        public int ImgWidthExtraLarge { get; set; }

        [HtmlAttributeName("width--xxl")]
        public int ImgWidthExtraExtraLarge { get; set; }

        [HtmlAttributeName("height")]
        public int ImgHeight { get; set; }

        [HtmlAttributeName("height--s")]
        public int ImgHeightSmall { get; set; }

        [HtmlAttributeName("height--m")]
        public int ImgHeightMedium { get; set; }

        [HtmlAttributeName("height--l")]
        public int ImgHeightLarge { get; set; }

        [HtmlAttributeName("height--xl")]
        public int ImgHeightExtraLarge { get; set; }

        [HtmlAttributeName("height--xxl")]
        public int ImgHeightExtraExtraLarge { get; set; }

        [HtmlAttributeName("cropalias")]
        public string ImgCropAlias { get; set; }

        [HtmlAttributeName("cropalias--s")]
        public string ImgCropAliasSmall { get; set; }

        [HtmlAttributeName("cropalias--m")]
        public string ImgCropAliasMedium { get; set; }

        [HtmlAttributeName("cropalias--l")]
        public string ImgCropAliasLarge { get; set; }

        [HtmlAttributeName("cropalias--xl")]
        public string ImgCropAliasExtraLarge { get; set; }

        [HtmlAttributeName("cropalias--xxl")]
        public string ImgCropAliasExtraExtraLarge { get; set; }

        [HtmlAttributeName("style")]
        public string? ImgStyle { get; set; }

        [HtmlAttributeName("class")]
        public string? ImgClass { get; set; }

        [HtmlAttributeName("abovethefold")]
        public bool AboveTheFold { get; set; }

        protected HttpRequest Request => ViewContext.HttpContext.Request;

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "img";

            #region Can only use media-item OR src, don't render anything if both are provided
            // Can't use both properties together
            if (string.IsNullOrWhiteSpace(FileSource) == false && MediaItem is not null)
            {
                // KABOOM !
                // Can't decide which property to use to render image
                // So just render nothing...
                output.SuppressOutput();
                return;
            }
            #endregion

            var width = 0d;
            var height = 0d;
            var cssClasses = new List<string>();
            var imgSrc = string.Empty;
            var placeholderImgSrc = string.Empty;
            var jsLazyLoad = !_globalSettings.OurImg.UseNativeLazyLoading && !AboveTheFold;
            var style = ImgStyle;

            if (MediaItem is not null)
            {
                #region Opting to use a media-item as the source image
                var media = _mediaService.GetById(MediaItem.Id); // Get the media object from the media library service
                var originalWidth = media.GetValue<double>("umbracoWidth"); // Determine the width from the originally uploaded image
                var originalHeight = media.GetValue<double>("umbracoHeight"); // Determine the height from the originally uploaded image
                width = ImgWidth > 0 ? ImgWidth : originalWidth; // If the element wasn't provided with a width property, use the width from the media object instead
                if (!string.IsNullOrEmpty(ImgCropAlias))
                {
                    // The element contains a crop alias property, so pull through a cropped version of the original image
                    // Also, calculate the height based on the given width using the crop profile so it's to scale
                    imgSrc = MediaItem.GetCropUrl(width: (int)width, cropAlias: ImgCropAlias);
                    if (_globalSettings.OurImg.LazyLoadPlaceholder.Equals(ImagePlaceholderType.LowQualityImage))
                    {
                        // Generate a low quality placeholder image if configured to do so
                        placeholderImgSrc = MediaItem.GetCropUrl(width: ImgWidth, cropAlias: ImgCropAlias, quality: _globalSettings.OurImg.LazyLoadPlaceholderLowQualityImageQuality);
                    }
                    var cropWidth = MediaItem.LocalCrops.GetCrop(ImgCropAlias).Width;
                    var cropHeight = MediaItem.LocalCrops.GetCrop(ImgCropAlias).Height;
                    height = (cropHeight / cropWidth) * width;
                }
                else
                {
                    // Pull through an image based on the given width and calculate the height so it's to scale.
                    imgSrc = MediaItem.GetCropUrl(width: (int)width);
                    if (_globalSettings.OurImg.LazyLoadPlaceholder.Equals(ImagePlaceholderType.LowQualityImage))
                    {
                        // Generate a low quality placeholder image if configured to do so
                        placeholderImgSrc = MediaItem.GetCropUrl(width: (int)width, quality: _globalSettings.OurImg.LazyLoadPlaceholderLowQualityImageQuality);
                    }
                    height = (originalHeight / originalWidth) * width;
                }

                #region Autogenerate alt text
                if (string.IsNullOrWhiteSpace(ImgAlt))
                {
                    output.Attributes.Add("alt", GetImageAltText(MediaItem));
                }
                #endregion
                #endregion
            }
            else if (!string.IsNullOrEmpty(FileSource))
            {
                #region Opting to use a file URL as the source image
                width = ImgWidth;
                height = ImgHeight;

                imgSrc = AddQueryToUrl(FileSource, "width", width.ToString());

                #region Autogenerate alt text
                if (string.IsNullOrWhiteSpace(ImgAlt))
                {
                    output.Attributes.Add("alt", GetImageAltText(FileSource));
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

                #endregion
            }

            #region Apply the width & height properties
            if (width > 0 && height > 0)
            {
                output.Attributes.Add("width", width);
                output.Attributes.Add("height", height);
            }
            #endregion

            #region Apply the aspect-ratio style if configured to do so. 
            /// Having width & height by themselves forces the image to initially load as that size during page load until a stylesheet kicks in.
            /// PageSpeed Insights requires all images to have a width & height.
            /// aspect-ratio sizes the element consistently, so the ratio of an element stays the same as it grows or shrinks.
            if (width > 0 && height > 0 && _globalSettings.OurImg.ApplyAspectRatio)
            {
                var aspectRatio = $"aspect-ratio: {width} / {height};";
                style = style?.Trim().TrimEnd(';');
                if (!string.IsNullOrEmpty(ImgStyle))
                {
                    style += ";";
                    output.Attributes.RemoveAll("style");
                }
                style += aspectRatio;
                style += "width: 100%; height: auto;";
                output.Attributes.Add("style", style);
                output.Attributes.RemoveAll("width");
                output.Attributes.RemoveAll("height");
            }
            #endregion

            #region If we're lazy loading via a JavaScript method, set a placeholder on the 'src' property and set the image to use 'data-src' instead.

            if (jsLazyLoad)
            {
                output.Attributes.Add("data-src", imgSrc);
                if (_globalSettings.OurImg.LazyLoadPlaceholder.Equals(ImagePlaceholderType.LowQualityImage) && !string.IsNullOrEmpty(placeholderImgSrc))
                {
                    output.Attributes.Add("src", placeholderImgSrc);
                }
                else if (width > 0 && height > 0)
                {
                    output.Attributes.Add("src", $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'%3E%3C/svg%3E");
                }
                cssClasses.Add(_globalSettings.OurImg.LazyLoadCssClass);
            }
            else
            {
                output.Attributes.Add("src", imgSrc);
            }
            #endregion

            #region If we're instead lazy loading via a browser method, add properties to assist the load order
            if (_globalSettings.OurImg.UseNativeLazyLoading || !jsLazyLoad)
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
            #endregion

            #region Apply CSS classes
            if (!string.IsNullOrEmpty(ImgClass))
            {
                cssClasses.Add(ImgClass);
            }

            if (cssClasses.Count > 0)
            {
                output.Attributes.Add("class", string.Join(" ", cssClasses));
            }
            #endregion

            #region If multiple responsive image variants have been supplied, wrap the img element with a picture element and source elements per variant.
            // Only one image will be rendered at a given time based on the current screen width. 
            // The configuration allows us to define whether images are configured "mobile first". This simply alternates between min-width & max-width media queries.
            var imageSizes = GetImageSizes(MediaItem != null);

            if (imageSizes != null && imageSizes.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("<picture>");

                imageSizes = _globalSettings.OurImg.MobileFirst ? imageSizes.OrderByDescending(o => o.ScreenSize).ToList() : imageSizes.OrderBy(o => o.ScreenSize).ToList();
                foreach (var size in imageSizes)
                {
                    var minWidth = size.ScreenSize switch
                    {
                        OurScreenSize.ExtraExtraLarge => _globalSettings.OurImg.MediaQueries.ExtraExtraLarge,
                        OurScreenSize.ExtraLarge => _globalSettings.OurImg.MediaQueries.ExtraLarge,
                        OurScreenSize.Large => _globalSettings.OurImg.MediaQueries.Large,
                        OurScreenSize.Medium => _globalSettings.OurImg.MediaQueries.Medium,
                        OurScreenSize.Small => _globalSettings.OurImg.MediaQueries.Small,
                        _ => 0
                    };

                    double sourceHeight = 0;

                    if (MediaItem != null)
                    {
                        #region Configure crops which can be set at variant level or inherit from the crop alias defined on the main img element itself. If neither have a crop alias, then don't use crops.
                        var cropAlias = !string.IsNullOrEmpty(size.CropAlias) ?
                            size.CropAlias :
                            !string.IsNullOrEmpty(ImgCropAlias) ?
                                ImgCropAlias :
                                null;
                        #endregion

                        if (!string.IsNullOrEmpty(cropAlias))
                        {
                            var cropWidth = MediaItem.LocalCrops.GetCrop(cropAlias).Width;
                            var cropHeight = MediaItem.LocalCrops.GetCrop(cropAlias).Height;
                            sourceHeight = (StringUtils.GetDouble(cropHeight) / StringUtils.GetDouble(cropWidth)) * size.ImageWidth;
                        }

                        sb.AppendLine($"<source {(jsLazyLoad ? "data-" : "")}srcset=\"{MediaItem.GetCropUrl(width: size.ImageWidth, cropAlias: cropAlias)}\" media=\"({(_globalSettings.OurImg.MobileFirst ? $"min-width: {minWidth}" : $"max-width: {minWidth - 1}")}px)\" width=\"{size.ImageWidth}\"{(sourceHeight > 0 ? $" height=\"{sourceHeight}\"" : "")} />");
                    }

                    if (!string.IsNullOrEmpty(FileSource) && ImgWidth > 0 && ImgHeight > 0)
                    {
                        sourceHeight = size.ImageHeight > 0 ? size.ImageHeight : (ImgHeight / ImgWidth) * size.ImageWidth;
                        var sourceUrl = AddQueryToUrl(FileSource, "width", size.ImageWidth.ToString());
                        sb.AppendLine($"<source {(jsLazyLoad ? "data-" : "")}srcset=\"{sourceUrl}\" media=\"({(_globalSettings.OurImg.MobileFirst ? $"min-width: {minWidth}" : $"max-width: {minWidth - 1}")}px)\" width=\"{size.ImageWidth}\"{(sourceHeight > 0 ? $" height=\"{sourceHeight}\"" : "")} />");

                    }
                }
                output.PreElement.SetHtmlContent(sb.ToString());
                output.PostElement.SetHtmlContent("</picture>");
            }
            #endregion
        }

        #region Private Methods
        private string GetImageAltText(string url)
        {
            try
            {
                if (url.Contains("://"))
                {
                    var uri = new Uri(url);
                    return Path.GetFileNameWithoutExtension(uri.LocalPath);
                }
                else
                {
                    var baseUri = new Uri(Request.GetDisplayUrl());
                    Uri uri;
                    if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                        uri = new Uri(baseUri, url);

                    return Path.GetFileNameWithoutExtension(uri.LocalPath);
                }

            }
            catch (Exception)
            {

            }

            return "";
        }
        private string GetImageAltText(IPublishedContent image)
        {
            try
            {
                if (image == null) throw new Exception("image is null");

                var alias = _globalSettings.OurImg.AlternativeTextMediaTypePropertyAlias;

                if (image.HasProperty(alias) && image.HasValue(alias))
                {
                    return image.Value<string>(alias);
                }
                else
                {
                    return image.Name;
                }
            }
            catch (Exception)
            {

            }

            return "";
        }

        private string AddQueryToUrl(string url, string key, string value)
        {
            Uri uri = null!;
            if (url.Contains("://"))
            {
                uri = new Uri(url);
            }
            else
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri!))
                    uri = new Uri(new Uri(Request.GetDisplayUrl()), url);
            }

            if (uri == null) return url;

            var baseUri = uri.GetComponents(UriComponents.Scheme | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.UriEscaped);
            var query = QueryHelpers.ParseQuery(uri.Query);

            var items = query.SelectMany(x => x.Value, (col, value) => new KeyValuePair<string, string>(col.Key, value)).ToList();

            items.RemoveAll(x => x.Key == key);

            var qb = new QueryBuilder(items);

            qb.Add(key, value);

            return baseUri + qb.ToQueryString();
        }

        private List<OurImageSize> GetImageSizes(bool isMedia = true)
        {
            var imageSizes = new List<OurImageSize>();

            if(ImgWidthSmall > 0)
            {
                imageSizes.Add(isMedia ? new OurImageSize(Enums.OurScreenSize.Small, ImgWidthSmall, ImgCropAliasSmall) : new OurImageSize(Enums.OurScreenSize.Small, ImgWidthSmall, ImgHeightSmall));
            }
            if(ImgWidthMedium > 0)
            {
                imageSizes.Add(isMedia ? new OurImageSize(Enums.OurScreenSize.Medium, ImgWidthMedium, ImgCropAliasMedium) : new OurImageSize(Enums.OurScreenSize.Medium, ImgWidthMedium, ImgHeightMedium));
            }
            if(ImgWidthLarge > 0)
            {
                imageSizes.Add(isMedia ? new OurImageSize(Enums.OurScreenSize.Large, ImgWidthLarge, ImgCropAliasLarge) : new OurImageSize(Enums.OurScreenSize.Large, ImgWidthLarge, ImgHeightLarge));
            }
            if(ImgWidthExtraLarge > 0)
            {
                imageSizes.Add(isMedia ? new OurImageSize(Enums.OurScreenSize.ExtraLarge, ImgWidthExtraLarge, ImgCropAliasExtraLarge) : new OurImageSize(Enums.OurScreenSize.ExtraLarge, ImgWidthExtraLarge, ImgHeightExtraLarge));
            }
            if(ImgWidthExtraExtraLarge > 0)
            {
                imageSizes.Add(isMedia ? new OurImageSize(Enums.OurScreenSize.ExtraExtraLarge, ImgWidthExtraExtraLarge, ImgCropAliasExtraExtraLarge) : new OurImageSize(Enums.OurScreenSize.ExtraExtraLarge, ImgWidthExtraExtraLarge, ImgHeightExtraExtraLarge));
            }

            return imageSizes;
        }
        #endregion
    }
}
