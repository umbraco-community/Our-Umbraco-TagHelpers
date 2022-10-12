using Our.Umbraco.TagHelpers.Enums;

namespace Our.Umbraco.TagHelpers.Classes
{
    internal class OurImageSize
    {
        public OurImageSize() { }
        public OurImageSize(OurScreenSize screenSize, int imageWidth, string? cropAlias = null)
        {
            ScreenSize = screenSize;
            ImageWidth = imageWidth;
            CropAlias = cropAlias;
        }
        public OurImageSize(OurScreenSize screenSize, int imageWidth, int imageHeight)
        {
            ScreenSize = screenSize;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
        }
        public OurScreenSize ScreenSize { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public string? CropAlias { get; set; }
    }
}
