using System;
using Our.Umbraco.TagHelpers.Enums;

namespace Our.Umbraco.TagHelpers.Configuration
{
    public class OurUmbracoTagHelpersConfiguration
    {
        public InlineSvgTagHelperConfiguration OurSVG { get; set; } = new InlineSvgTagHelperConfiguration();
        public ImgTagHelperConfiguration OurImg { get; set; } = new ImgTagHelperConfiguration();

        public SelfHostTagHelperConfiguration OurSelfHost { get; set; } = new SelfHostTagHelperConfiguration();
    }

    public class InlineSvgTagHelperConfiguration
    {
        public bool EnsureViewBox { get; set; } = false;
        public bool Cache { get; set; } = false;
        public int CacheMinutes { get; set; } = 180;

        /// <summary>
        /// Scopes each inlined SVG's &lt;style&gt; blocks to that SVG, so class names such as .st0 from two SVGs on one
        /// page don't restyle each other. Off by default.
        /// </summary>
        public bool ScopeStyles { get; set; } = false;

        /// <summary>
        /// Gives the ids in each inlined SVG a prefix of their own (and updates the references to them), so SVGs on
        /// one page don't share gradients, clip paths or filters. Off by default.
        /// </summary>
        public bool UniqueIds { get; set; } = false;
    }

    public class ImgTagHelperConfiguration
    {
        /// <summary>
        /// Define the typical responsive breakpoints (S,M,L,XL,XXL) in which your website uses during screen resize
        /// </summary>
        public MediaQuerySizes MediaQueries { get; set; } = new MediaQuerySizes();

        /// <summary>
        /// If true, let the browser handle image lazy loading, otherwise disable to use a 3rd party JavaScript based library
        /// </summary>
        public bool UseNativeLazyLoading { get; set; } = true;

        /// <summary>
        /// Applicable if UseNativeLazyLoading is false
        /// </summary>
        public string LazyLoadCssClass { get; set; } = "lazyload";

        /// <summary>
        /// Applicable if UseNativeLazyLoading is false
        /// </summary>
        public ImagePlaceholderType LazyLoadPlaceholder { get; set; } = ImagePlaceholderType.SVG;

        /// <summary>
        /// Applicable if UseNativeLazyLoading is false & LazyLoadPlaceholder is LowQualityImage
        /// </summary>
        public int LazyLoadPlaceholderLowQualityImageQuality { get; set; } = 5;
        public bool ApplyAspectRatio { get; set; } = false;
        public bool MobileFirst { get; set; } = true;

        /// <summary>
        /// The property alias of the media type containing the alternative text value.
        /// </summary>
        public string AlternativeTextMediaTypePropertyAlias { get; set; } = "alternativeText";
    }
    public class MediaQuerySizes
    {
        public int Small { get; set; } = 576;
        public int Medium { get; set; } = 768;
        public int Large { get; set; } = 992;
        public int ExtraLarge { get; set; } = 1200;
        public int ExtraExtraLarge { get; set; } = 1400;
    }

    public class SelfHostTagHelperConfiguration
    {
        public string RootFolder { get; set; } = "/assets";

        /// <summary>
        /// Hosts that files may be downloaded from, e.g. "unpkg.com". When it's empty (the default), any host is allowed.
        /// A URL on another host isn't downloaded; the tag keeps pointing at the original.
        /// </summary>
        public string[] AllowedHosts { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The largest file that's downloaded, in kilobytes (default 1024, 1 MB). Larger files keep the original URL;
        /// raise it to self host bigger files such as large images.
        /// </summary>
        public int MaxFileSizeKilobytes { get; set; } = 1024;

        /// <summary>
        /// How long to wait for a download, in seconds (default 30). A slower download keeps the original URL.
        /// </summary>
        public int DownloadTimeoutSeconds { get; set; } = 30;
    }
}
