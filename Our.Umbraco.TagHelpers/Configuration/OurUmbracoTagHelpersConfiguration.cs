using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Configuration
{
    public class OurUmbracoTagHelpersConfiguration
    {
        public InlineSvgTagHelperConfiguration InlineSvgTagHelper { get; set; } = new InlineSvgTagHelperConfiguration();
        public ImgTagHelperConfiguration ImgTagHelper { get; set; } = new ImgTagHelperConfiguration();
    }

    public class InlineSvgTagHelperConfiguration
    {
        public bool EnsureViewBox { get; set; } = false;
        public bool Cache { get; set; } = false;
        public int CacheMinutes { get; set; } = 180;
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
    }
    public class MediaQuerySizes
    {
        public int Small { get; set; } = 576;
        public int Medium { get; set; } = 768;
        public int Large { get; set; } = 992;
        public int ExtraLarge { get; set; } = 1200;
        public int ExtraExtraLarge { get; set; } = 1400;
    }

    public class OurImageSize
    {
        public OurImageSize() { }
        public OurImageSize(OurScreenSize screenSize, int imageWidth, string? cropAlias = null)
        {
            ScreenSize = screenSize;
            ImageWidth = imageWidth;
            CropAlias = cropAlias;
        }
        public OurScreenSize ScreenSize { get; set; }
        public int ImageWidth { get; set; }
        public string? CropAlias { get; set; }
    }

    public enum OurScreenSize
    {
        Small = 100,
        Medium = 200,
        Large = 300,
        ExtraLarge = 400,
        ExtraExtraLarge = 500
    }
    public enum ImagePlaceholderType
    {
        SVG,
        LowQualityImage
    }
}
