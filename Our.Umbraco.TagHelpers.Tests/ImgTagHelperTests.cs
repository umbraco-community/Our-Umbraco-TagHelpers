#nullable enable
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
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
    public class ImgTagHelperTests
    {
        private TagHelperContext _tagHelperContext = null!;

        // What the tag helper asked Umbraco's image URL generator for
        private ImageUrlGenerationOptions? _requested;

        [SetUp]
        public void SetUp()
        {
            _tagHelperContext = TestContextHelpers.GetTagHelperContext("img");
        }

        [TestCase("/", "/images/logo.png")]
        [TestCase("/some/child/page", "/images/logo.png")]
        [TestCase("/some/deeply/nested/child/page", "/images/logo.png")]
        public async Task FileSource_RootRelativePath_PreservesLeadingSlash_RegardlessOfCurrentRequestPath(string requestPath, string src)
        {
            // A src starting with "/" should always resolve relative to the site root,
            // not relative to whatever page the <our-img> tag happens to be rendered on.
            var tagHelper = CreateTagHelper(requestPath);
            tagHelper.FileSource = src;
            tagHelper.ImgWidth = 100;
            tagHelper.ImgHeight = 50;

            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            ClassicAssert.AreEqual(src, _requested!.ImageUrl);
        }

        [Test]
        public async Task FileSource_RelativePathWithoutLeadingSlash_ResolvesAgainstCurrentPage()
        {
            var tagHelper = CreateTagHelper("/some/child/page");
            tagHelper.FileSource = "images/logo.png";
            tagHelper.ImgWidth = 100;
            tagHelper.ImgHeight = 50;

            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            ClassicAssert.AreEqual("/some/child/images/logo.png", _requested!.ImageUrl);
        }

        [Test]
        public async Task FileSource_AbsoluteUrl_IsUnaffectedByCurrentRequestPath()
        {
            var tagHelper = CreateTagHelper("/some/child/page");
            tagHelper.FileSource = "https://cdn.example.com/images/logo.png";
            tagHelper.ImgWidth = 100;
            tagHelper.ImgHeight = 50;

            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            ClassicAssert.AreEqual("https://cdn.example.com/images/logo.png", _requested!.ImageUrl);
        }

        // Umbraco 17+ sites sign image requests with an HMAC (Umbraco:CMS:Imaging:HMACSecretKey, generated
        // on install) and ImageSharp answers 400 to an unsigned URL, so file URLs must come from Umbraco's generator.
        [Test]
        public async Task FileSource_Url_ComesFromUmbracosImageUrlGenerator()
        {
            const string signedUrl = "/images/logo.png?width=100&height=50&hmac=signed";
            var tagHelper = CreateTagHelper("/some/child/page", generatedUrl: signedUrl);
            tagHelper.FileSource = "/images/logo.png";
            tagHelper.ImgWidth = 100;
            tagHelper.ImgHeight = 50;

            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            ClassicAssert.AreEqual(signedUrl, output.Attributes["src"].Value.ToString());
            ClassicAssert.AreEqual("/images/logo.png", _requested!.ImageUrl);
            ClassicAssert.AreEqual(100, _requested.Width);
            ClassicAssert.AreEqual(50, _requested.Height);
            ClassicAssert.IsNull(_requested.FurtherOptions);
        }

        [Test]
        public async Task FileSource_Url_KeepsItsOwnQueryButNotItsWidthAndHeight()
        {
            var tagHelper = CreateTagHelper("/");
            tagHelper.FileSource = "/images/logo.png?format=webp&width=1&height=2";
            tagHelper.ImgWidth = 100;
            tagHelper.ImgHeight = 50;

            var output = TestContextHelpers.GetTagHelperOutput("our-img");
            await tagHelper.ProcessAsync(_tagHelperContext, output);

            ClassicAssert.AreEqual("/images/logo.png", _requested!.ImageUrl);
            ClassicAssert.AreEqual("format=webp", _requested.FurtherOptions);
            ClassicAssert.AreEqual(100, _requested.Width);
            ClassicAssert.AreEqual(50, _requested.Height);
        }

        private ImgTagHelper CreateTagHelper(string requestPath, string generatedUrl = "/generated.png")
        {
            var globalSettings = Options.Create(new OurUmbracoTagHelpersConfiguration());

            var imageUrlGenerator = new Mock<IImageUrlGenerator>();
            imageUrlGenerator
                .Setup(g => g.GetImageUrl(It.IsAny<ImageUrlGenerationOptions>()))
                .Callback<ImageUrlGenerationOptions>(options => _requested = options)
                .Returns(generatedUrl);

            var tagHelper = new ImgTagHelper(globalSettings, imageUrlGenerator.Object)
            {
                ViewContext = CreateViewContext(requestPath)
            };

            return tagHelper;
        }

        private static ViewContext CreateViewContext(string requestPath)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");
            httpContext.Request.Path = requestPath;

            return new ViewContext { HttpContext = httpContext };
        }
    }
}
