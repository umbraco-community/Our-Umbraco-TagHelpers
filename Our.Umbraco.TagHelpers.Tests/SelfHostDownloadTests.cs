#nullable enable
using System;
using System.IO;
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
                    if (path.EndsWith("/missing.js")) { context.Response.StatusCode = 404; }
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

        private SelfHostService Service(Action<SelfHostTagHelperConfiguration>? configure = null)
        {
            var settings = new SelfHostTagHelperConfiguration { RootFolder = "/assets", MaxFileSizeKilobytes = 10, DownloadTimeoutSeconds = 10 };
            configure?.Invoke(settings);
            var hostEnv = Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == _webRoot && e.ContentRootPath == _webRoot);
            return new SelfHostService(
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IAppPolicyCache>(),
                hostEnv,
                Options.Create(new OurUmbracoTagHelpersConfiguration { OurSelfHost = settings }));
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
