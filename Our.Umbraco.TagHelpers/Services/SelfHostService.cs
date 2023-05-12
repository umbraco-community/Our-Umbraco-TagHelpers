using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Extensions;
using Our.Umbraco.TagHelpers.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Services
{
    public class SelfHostService : ISelfHostService
    {
        private readonly IProfilingLogger _logger;
        private readonly IAppPolicyCache _runtimeCache;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly OurUmbracoTagHelpersConfiguration _globalSettings;

        public SelfHostService(
            IProfilingLogger logger,
            IAppPolicyCache appPolicyCache,
            IWebHostEnvironment hostingEnvironment,
            IOptions<OurUmbracoTagHelpersConfiguration> globalSettings
            )
        {
            _logger = logger;
            _runtimeCache = appPolicyCache;
            _hostingEnvironment = hostingEnvironment;
            _globalSettings = globalSettings.Value;
        }

        public async Task<SelfHostedFile> SelfHostFile(string url, string? subfolder = null, string? fileExtension = null)
        {
            return await _runtimeCache.GetCacheItem($"Our.Umbraco.TagHelpers.Services.SelfHostService.SelfHostedFile({url}, {subfolder}, {fileExtension})", async () =>
            {
                using (_logger.TraceDuration<ISelfHostService>($"Start generating SelfHostedFile: {url}", $"Finished generating SelfHostedFile: {url}"))
                {
                    var uri = new Uri(url, UriKind.Absolute);

                    var selfHostedFile = new SelfHostedFile()
                    {
                        ExternalUrl = url,
                        FileName = uri.Segments.Last() + fileExtension.IfNotNull(ext => ext.EnsureStartsWith(".")),
                        FolderPath = GetFolderPath(uri, subfolder)
                    };

                    selfHostedFile.Url = await GetSelfHostedUrl(selfHostedFile);
                    return selfHostedFile;
                }
            });
        }

        public string GetFolderPath(Uri uri, string? subfolder = null)
        {
            var folderPath = _globalSettings.OurSelfHost.RootFolder;

            if (subfolder.IsNullOrWhiteSpace() == false) folderPath += subfolder.EnsureStartsWith("/");

            folderPath += GetRemoteFolderPath(uri);

            return folderPath;
        }

        public string GetRemoteFolderPath(Uri uri)
        {
            var segments = uri?.Segments;

            // if there is more than 2 segments (first segment is the root, last segment is the file)
            // we can extract the folderpath
            if (segments?.Length > 2)
            {
                segments = segments.Skip(1).SkipLast(1).ToArray();

                // remove trailing slash from segments
                segments = segments.Select(x => x.Replace("/", "")).ToArray();

                // join segments with slash
                return string.Join("/", segments).EnsureStartsWith("/");
            }

            return string.Empty;
        }
        private async Task<string> GetSelfHostedUrl(SelfHostedFile file)
        {
            var filePath = $"{file.FolderPath}/{file.FileName}";
            var localPath = _hostingEnvironment.MapPathWebRoot(file.FolderPath);
            var localFilePath = _hostingEnvironment.MapPathWebRoot(filePath);

            if (!File.Exists(localFilePath) && file.ExternalUrl is not null)
            {
                using (_logger.TraceDuration<ISelfHostService>($"Start downloading SelfHostedFile: {file.ExternalUrl} to {localFilePath}", $"Finished downloading SelfHostedFile: {file.ExternalUrl} to {localFilePath}"))
                {
                    var content = await GetUrlContent(file.ExternalUrl);
                    if (content != null)
                    {
                        if (!Directory.Exists(localPath)) Directory.CreateDirectory(localPath);
                        await File.WriteAllBytesAsync(localFilePath, content);
                        return filePath;
                    }
                    else
                    {
                        return file.ExternalUrl;
                    }
                }
            }

            return filePath;
        }

        private static async Task<byte[]?> GetUrlContent(string url)
        {
            using (var client = new HttpClient())
            {
                using (var result = await client.GetAsync(url))
                {
                    if (result is not null && result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsByteArrayAsync();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}
