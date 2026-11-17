using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Our.Umbraco.TagHelpers.Configuration;
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

        // A file that couldn't be self hosted (the download failed, timed out or was too large) keeps its original URL
        // for this long, then the download is tried again. Self-hosted files stay cached.
        internal static readonly TimeSpan RetryFailedDownloadAfter = TimeSpan.FromMinutes(5);

        private static readonly object RetryLock = new();

        // The clock for retrying failed downloads; tests replace it to move time forward
        internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;

        // What's cached for a URL: the file, and when to try again if it wasn't self hosted
        private sealed record CachedFile(SelfHostedFile File, DateTimeOffset? RetryAfter);

        public async Task<SelfHostedFile> SelfHostFile(string url, string? subfolder = null, string? fileExtension = null)
        {
            async Task<CachedFile> CreateCachedFile()
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

                    // Still the original URL: try again later, rather than not until the site restarts
                    var retryAfter = selfHostedFile.Url == url ? TimeProvider.GetUtcNow() + RetryFailedDownloadAfter : (DateTimeOffset?)null;
                    return new CachedFile(selfHostedFile, retryAfter);
                }
            }

            // Only http(s) URLs on an allowed host are downloaded; anything else keeps pointing at the original
            if (!IsAllowedUrl(url))
            {
                return new SelfHostedFile { ExternalUrl = url, Url = url };
            }

            var cacheKey = $"Our.Umbraco.TagHelpers.Services.SelfHostService.SelfHostedFile({url}, {subfolder}, {fileExtension})";
            var cached = _runtimeCache.GetCacheItem(cacheKey, CreateCachedFile);
            var entry = await (cached ?? CreateCachedFile());

            if (cached is not null && entry.RetryAfter is { } retryAfter && TimeProvider.GetUtcNow() >= retryAfter)
            {
                // Only the first request to find the old entry removes it; requests at the same time then share the
                // one new download, as on the first render
                lock (RetryLock)
                {
                    if (ReferenceEquals(_runtimeCache.Get(cacheKey), cached))
                    {
                        _runtimeCache.Clear(cacheKey);
                    }
                }

                entry = await (_runtimeCache.GetCacheItem(cacheKey, CreateCachedFile) ?? CreateCachedFile());
            }

            return entry.File;
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
            var localPath = _hostingEnvironment.MapPathWebRoot(file.FolderPath ?? string.Empty);
            var localFilePath = _hostingEnvironment.MapPathWebRoot(filePath);

            // The file must stay inside the root folder (a folder="../.." attribute must not write elsewhere)
            if (!IsInsideRootFolder(localFilePath))
            {
                Logger?.LogWarning("Not self hosting {Url}: {Path} is outside the {RootFolder} folder", file.ExternalUrl, filePath, _globalSettings.OurSelfHost.RootFolder);
                return file.ExternalUrl ?? filePath;
            }

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

        // One HttpClient for every download, as Microsoft recommends: a new one per download can exhaust sockets, and
        // PooledConnectionLifetime replaces connections every few minutes so DNS changes (e.g. a CDN moving) are picked
        // up. The timeout is per download (DownloadTimeoutSeconds), so the client's own timeout is off.
        private static readonly HttpClient HttpClient = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        // Logging without changing the public constructor (IProfilingLogger can't log warnings)
        private static ILogger? Logger => StaticServiceProvider.Instance?.GetService<ILogger<SelfHostService>>();

        private bool IsAllowedUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }

            var allowedHosts = _globalSettings.OurSelfHost.AllowedHosts;
            if (allowedHosts is { Length: > 0 } && !allowedHosts.Any(host => string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase)))
            {
                Logger?.LogWarning("Not self hosting {Url}: {Host} isn't in OurSelfHost:AllowedHosts", url, uri.Host);
                return false;
            }

            return true;
        }

        private bool IsInsideRootFolder(string localFilePath)
        {
            var root = Path.GetFullPath(_hostingEnvironment.MapPathWebRoot(_globalSettings.OurSelfHost.RootFolder))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(localFilePath).StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Downloads the file, or returns null when the download fails, takes longer than DownloadTimeoutSeconds
        /// or is larger than MaxFileSizeKilobytes. The tag then keeps the original URL instead of breaking the page.
        /// </summary>
        private async Task<byte[]?> GetUrlContent(string url)
        {
            var settings = _globalSettings.OurSelfHost;
            var maxBytes = (long)settings.MaxFileSizeKilobytes * 1024;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.DownloadTimeoutSeconds));
                using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Logger?.LogWarning("Not self hosting {Url}: the download returned {StatusCode}", url, (int)response.StatusCode);
                    return null;
                }

                if (response.Content.Headers.ContentLength > maxBytes)
                {
                    Logger?.LogWarning("Not self hosting {Url}: it's larger than OurSelfHost:MaxFileSizeKilobytes ({MaxKilobytes} KB)", url, settings.MaxFileSizeKilobytes);
                    return null;
                }

                // Read at most one byte more than allowed, in case the size wasn't sent
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var content = new MemoryStream();
                var buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, timeout.Token)) > 0)
                {
                    content.Write(buffer, 0, read);
                    if (content.Length > maxBytes)
                    {
                        Logger?.LogWarning("Not self hosting {Url}: it's larger than OurSelfHost:MaxFileSizeKilobytes ({MaxKilobytes} KB)", url, settings.MaxFileSizeKilobytes);
                        return null;
                    }
                }

                return content.ToArray();
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
            {
                Logger?.LogWarning(ex, "Not self hosting {Url}: the download failed or took longer than OurSelfHost:DownloadTimeoutSeconds ({Seconds} s)", url, settings.DownloadTimeoutSeconds);
                return null;
            }
        }
    }
}
