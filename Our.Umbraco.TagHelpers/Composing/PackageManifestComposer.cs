using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Composing
{
    public class PackageManifestComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IPackageManifestReader, TagHelperManifestReader>();
        }
    }

    public class TagHelperManifestReader : IPackageManifestReader
    {
        public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
        {
            // Used to register a package manifest via C#
            // As this package contains no backoffice extensions we are registering it via C# and not JS or JSON file
            // User can then see this as intalled package in the backoffice and telemetry can be sent to Umbraco
            var version = typeof(TagHelperManifestReader).Assembly.GetName()?.Version?.ToString() ?? "Unknown";
            var manifest = new PackageManifest
            {
                Id = "Our.Umbraco.TagHelpers",
                Name = "Our Umbraco TagHelpers",
                AllowTelemetry = true,
                Version = version,
                Extensions = []
            };
            
            return Task.FromResult(manifest.AsEnumerableOfOne());
        }
    }
}
