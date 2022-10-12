namespace Our.Umbraco.TagHelpers.Configuration
{
    public class OurUmbracoTagHelpersConfiguration
    {
        public InlineSvgTagHelperConfiguration OurSVG { get; set; } = new InlineSvgTagHelperConfiguration();
    }

    public class InlineSvgTagHelperConfiguration
    {
        public bool EnsureViewBox { get; set; } = false;
        public bool Cache { get; set; } = false;
        public int CacheMinutes { get; set; } = 180;
    }
}
