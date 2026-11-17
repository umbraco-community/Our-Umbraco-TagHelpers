#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Models;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/51
    // <our-img src="/images/a-missing-image.jpg" fallback-img="/images/not-found.jpg" />
    public class ImgTagHelperFallbackTests
    {
        private const string Fallback = "/images/not-found.jpg";

        // The web root has /images/logo.png and /images/not-found.jpg
        private Mock<IFileProvider> _webRoot = null!;
        private ImageUrlGenerationOptions? _requested;

        [SetUp]
        public void SetUp()
        {
            _webRoot = new Mock<IFileProvider>();
            _webRoot.Setup(p => p.GetFileInfo(It.IsAny<string>())).Returns(Mock.Of<IFileInfo>(f => f.Exists == false));
            foreach (var path in new[] { "/images/logo.png", Fallback })
            {
                _webRoot.Setup(p => p.GetFileInfo(path)).Returns(Mock.Of<IFileInfo>(f => f.Exists == true));
            }
            _requested = null;
        }

        private async Task<TagHelperOutput> Render(string? src, string? fallback, MediaWithCrops? mediaItem = null, int width = 0, int height = 0)
        {
            var imageUrlGenerator = new Mock<IImageUrlGenerator>();
            imageUrlGenerator.Setup(g => g.GetImageUrl(It.IsAny<ImageUrlGenerationOptions>()))
                .Callback<ImageUrlGenerationOptions>(options => _requested = options)
                .Returns("/generated.png");

            var services = new ServiceCollection()
                .AddSingleton(Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == _webRoot.Object))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = services };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.Path = "/";

            var tagHelper = new ImgTagHelper(Options.Create(new OurUmbracoTagHelpersConfiguration()), imageUrlGenerator.Object)
            {
                ViewContext = new ViewContext { HttpContext = httpContext },
                FileSource = src,
                MediaItem = mediaItem,
                FallbackImage = fallback,
                ImgWidth = width,
                ImgHeight = height,
            };
            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(TestContextHelpers.GetTagHelperContext("img"), output);
            return output;
        }

        private static string? Src(TagHelperOutput output) => output.Attributes["src"]?.Value?.ToString();

        [Test]
        public async Task NoMediaItemPicked_UsesTheFallback()
        {
            ClassicAssert.AreEqual(Fallback, Src(await Render(src: null, fallback: Fallback)));
        }

        [Test]
        public async Task SrcFileMissing_UsesTheFallback()
        {
            ClassicAssert.AreEqual(Fallback, Src(await Render("/images/a-missing-image.jpg", Fallback)));
        }

        [Test]
        public async Task SrcFileExists_IsKept()
        {
            ClassicAssert.AreEqual("/images/logo.png", Src(await Render("/images/logo.png", Fallback)));
        }

        [Test]
        public async Task SrcWithAQueryString_ChecksOnlyThePath()
        {
            ClassicAssert.AreEqual("/images/logo.png?v=2", Src(await Render("/images/logo.png?v=2", Fallback)));
        }

        [TestCase("https://cdn.example.com/images/missing.jpg")]
        [TestCase("//cdn.example.com/images/missing.jpg")]
        public async Task ExternalSrc_IsNotChecked(string src)
        {
            ClassicAssert.AreEqual(src, Src(await Render(src, Fallback)));
            _webRoot.Verify(p => p.GetFileInfo(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task WithoutAFallback_NothingIsChecked_AndTheSrcIsKept()
        {
            ClassicAssert.AreEqual("/images/a-missing-image.jpg", Src(await Render("/images/a-missing-image.jpg", fallback: null)));
            _webRoot.Verify(p => p.GetFileInfo(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task Fallback_IsResizedLikeAnySrc()
        {
            var output = await Render("/images/a-missing-image.jpg", Fallback, width: 200, height: 100);

            ClassicAssert.AreEqual(Fallback, _requested!.ImageUrl);
            ClassicAssert.AreEqual(200, _requested.Width);
        }
    }
}
