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
    }

    public class InlineSvgTagHelperConfiguration
    {
        public bool EnsureViewBox { get; set; } = false;
        public bool Cache { get; set; } = false;
        public int CacheMinutes { get; set; } = 180;
    }
}
