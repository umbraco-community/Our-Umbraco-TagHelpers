#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Services;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/118
    // Real downloads from a local HTTP server: what our-self-host saves, and when it keeps the original URL
    public class SelfHostDownloadTests
    {
        private HttpListener _server = null!;
        private string _baseUrl = null!;
        private string _webRoot = null!;

        // flaky.js answers 503 until _flakyUp is set, and counts its requests
        private volatile bool _flakyUp;
        private int _flakyRequests;

        [OneTimeSetUp]
        public void StartServer()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            _baseUrl = $"http://localhost:{port}";
            _server = new HttpListener();
            _server.Prefixes.Add(_baseUrl + "/");
            _server.Start();
            _ = Task.Run(ServeAsync);
        }

        [OneTimeTearDown]
        public void StopServer() => _server.Close();

        private async Task ServeAsync()
        {
            while (_server.IsListening)
            {
                HttpListenerContext context;
                try { context = await _server.GetContextAsync(); } catch { return; }
                var path = context.Request.Url!.AbsolutePath;
                try
                {
                    if (path.EndsWith("/flaky.js")) Interlocked.Increment(ref _flakyRequests);
                    if (path.EndsWith("/missing.js")) { context.Response.StatusCode = 404; }
                    else if (path.EndsWith("/flaky.js") && !_flakyUp) { context.Response.StatusCode = 503; }
                    else
                    {
                        if (path.EndsWith("/slow.js")) await Task.Delay(TimeSpan.FromSeconds(3));
                        var size = path.EndsWith("/big.js") ? 20 * 1024 : 100;
                        var body = Encoding.ASCII.GetBytes(new string('a', size));
                        context.Response.ContentLength64 = body.Length;
                        await context.Response.OutputStream.WriteAsync(body);
                    }
                }
                catch { }
                finally { try { context.Response.Close(); } catch { } }
            }
        }

        [SetUp]
        public void CreateWebRoot() => _webRoot = Directory.CreateTempSubdirectory("self-host-").FullName;

        [TearDown]
        public void DeleteWebRoot() => Directory.Delete(_webRoot, recursive: true);

        private sealed class TestClock : TimeProvider
        {
            private DateTimeOffset _now = DateTimeOffset.UtcNow;
            public override DateTimeOffset GetUtcNow() => _now;
            public void Advance(TimeSpan time) => _now += time;
        }

        private SelfHostService Service(Action<SelfHostTagHelperConfiguration>? configure = null, IAppPolicyCache? cache = null, TimeProvider? clock = null)
        {
            var settings = new SelfHostTagHelperConfiguration { RootFolder = "/assets", MaxFileSizeKilobytes = 10, DownloadTimeoutSeconds = 10 };
            configure?.Invoke(settings);
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == _webRoot && e.ContentRootPath == _webRoot);
            return new SelfHostService(
                Mock.Of<IProfilingLogger>(),
                cache ?? Mock.Of<IAppPolicyCache>(),
                hostEnv,
                Options.Create(new OurUmbracoTagHelpersConfiguration { OurSelfHost = settings }))
            {
                TimeProvider = clock ?? TimeProvider.System,
            };
        }

        private bool Saved(string relativePath) => File.Exists(Path.Combine(_webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        [Test]
        public void Defaults_DownloadAnyHost_UpTo1MB_For30Seconds()
        {
            var defaults = new SelfHostTagHelperConfiguration();
            CollectionAssert.IsEmpty(defaults.AllowedHosts);
            ClassicAssert.AreEqual(1024, defaults.MaxFileSizeKilobytes);
            ClassicAssert.AreEqual(30, defaults.DownloadTimeoutSeconds);
        }

        [Test]
        public async Task FailedDownload_IsTriedAgainAfter5Minutes_ThenTheSelfHostedFileIsKept()
        {
            // With Umbraco's real runtime cache: a failed download used to be cached until the site restarted
            var url = $"{_baseUrl}/lib/flaky.js";
            var clock = new TestClock();
            var service = Service(cache: new ObjectCacheAppCache(), clock: clock);
            _flakyUp = false;
            _flakyRequests = 0;

            // The server is down: the original URL is used
            ClassicAssert.AreEqual(url, (await service.SelfHostFile(url)).Url);
            ClassicAssert.AreEqual(1, _flakyRequests);

            // The server is back, but within 5 minutes the fallback is still cached, with no new download
            _flakyUp = true;
            clock.Advance(TimeSpan.FromMinutes(4));
            ClassicAssert.AreEqual(url, (await service.SelfHostFile(url)).Url);
            ClassicAssert.AreEqual(1, _flakyRequests);

            // After 5 minutes it's tried again, and the file is self hosted
            clock.Advance(TimeSpan.FromMinutes(1));
            ClassicAssert.AreEqual("/assets/lib/flaky.js", (await service.SelfHostFile(url)).Url);
            ClassicAssert.AreEqual(2, _flakyRequests);
            ClassicAssert.IsTrue(Saved("assets/lib/flaky.js"));

            // A self-hosted file stays cached: no more downloads
            clock.Advance(TimeSpan.FromHours(1));
            ClassicAssert.AreEqual("/assets/lib/flaky.js", (await service.SelfHostFile(url)).Url);
            ClassicAssert.AreEqual(2, _flakyRequests);
        }

        [Test]
        public async Task RequestsAtTheSameTime_After5Minutes_ShareOneNewDownload()
        {
            var url = $"{_baseUrl}/lib/flaky.js";
            var clock = new TestClock();
            var service = Service(cache: new ObjectCacheAppCache(), clock: clock);
            _flakyUp = false;
            _flakyRequests = 0;
            await service.SelfHostFile(url);

            _flakyUp = true;
            clock.Advance(TimeSpan.FromMinutes(5));
            var files = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => service.SelfHostFile(url))));

            ClassicAssert.AreEqual(2, _flakyRequests, "one download on the first render, one when it's tried again");
            ClassicAssert.IsTrue(files.All(f => f.Url == "/assets/lib/flaky.js"));
        }

        [Test]
        public async Task SmallFile_IsSavedAndServedLocally()
        {
            var file = await Service().SelfHostFile($"{_baseUrl}/lib/small.js");

            ClassicAssert.AreEqual("/assets/lib/small.js", file.Url);
            ClassicAssert.IsTrue(Saved("assets/lib/small.js"));
        }

        [Test]
        public async Task FileOverTheSizeLimit_KeepsTheOriginalUrl()
        {
            var url = $"{_baseUrl}/lib/big.js";
            var file = await Service().SelfHostFile(url);

            ClassicAssert.AreEqual(url, file.Url);
            ClassicAssert.IsFalse(Saved("assets/lib/big.js"));
        }

        [Test]
        public async Task SlowDownload_KeepsTheOriginalUrl()
        {
            // The server waits 3 seconds before answering slow.js
            var url = $"{_baseUrl}/lib/slow.js";
            var file = await Service(s => s.DownloadTimeoutSeconds = 1).SelfHostFile(url);

            ClassicAssert.AreEqual(url, file.Url);
            ClassicAssert.IsFalse(Saved("assets/lib/slow.js"));
        }

        [Test]
        public async Task FailedDownload_KeepsTheOriginalUrl()
        {
            var url = $"{_baseUrl}/lib/missing.js";
            ClassicAssert.AreEqual(url, (await Service().SelfHostFile(url)).Url);
        }

        [Test]
        public async Task UnreachableServer_KeepsTheOriginalUrlInsteadOfThrowing()
        {
            var url = "http://localhost:1/lib/x.js";
            ClassicAssert.AreEqual(url, (await Service().SelfHostFile(url)).Url);
        }

        [Test]
        public async Task HostNotInAllowedHosts_KeepsTheOriginalUrl()
        {
            var url = $"{_baseUrl}/lib/small.js";
            var file = await Service(s => s.AllowedHosts = new[] { "unpkg.com" }).SelfHostFile(url);

            ClassicAssert.AreEqual(url, file.Url);
            ClassicAssert.IsFalse(Saved("assets/lib/small.js"));
        }

        [Test]
        public async Task HostInAllowedHosts_IsSaved()
        {
            var file = await Service(s => s.AllowedHosts = new[] { "unpkg.com", "LOCALHOST" }).SelfHostFile($"{_baseUrl}/lib/small.js");

            ClassicAssert.AreEqual("/assets/lib/small.js", file.Url);
        }

        [Test]
        public async Task FolderOutsideTheRootFolder_KeepsTheOriginalUrl()
        {
            var url = $"{_baseUrl}/lib/small.js";
            var file = await Service().SelfHostFile(url, subfolder: "../../outside");

            ClassicAssert.AreEqual(url, file.Url);
            ClassicAssert.IsFalse(Directory.Exists(Path.Combine(Path.GetDirectoryName(_webRoot)!, "outside")));
        }

        [TestCase("ftp://example.com/lib/x.js")]
        [TestCase("file:///etc/hosts")]
        public async Task NonHttpUrl_KeepsTheOriginalUrl(string url)
        {
            ClassicAssert.AreEqual(url, (await Service().SelfHostFile(url)).Url);
        }
    }
}
