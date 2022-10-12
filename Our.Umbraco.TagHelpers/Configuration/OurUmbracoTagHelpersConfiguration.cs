using Our.Umbraco.TagHelpers.Enums;

namespace Our.Umbraco.TagHelpers.Configuration
{
    public class OurUmbracoTagHelpersConfiguration
    {
        public ImgTagHelperConfiguration OurImg { get; set; } = new ImgTagHelperConfiguration();
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
}
