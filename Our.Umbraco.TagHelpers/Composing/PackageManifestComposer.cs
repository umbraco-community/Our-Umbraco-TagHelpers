using System.Collections.Generic;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;

namespace Our.Umbraco.TagHelpers.Composing
{
    public class PackageManifestComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.ManifestFilters().Append<TagHelperManifestFilter>();
        }
    }

    public class TagHelperManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            var version = typeof(TagHelperManifestFilter).Assembly.GetName().Version.ToString();

            manifests.Add(new PackageManifest
            {
                PackageName = "Our.Umbraco.TagHelpers",
                AllowPackageTelemetry = true,
                Version = version
            });
        }
    }
}
