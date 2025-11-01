using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.TagHelpers.Configuration
{
    public class OurUmbracoTagHelpersConfigurationComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddOptions<OurUmbracoTagHelpersConfiguration>()
                .Bind(builder.Config.GetSection("Our.Umbraco.TagHelpers"));
        }
    }
}
