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
using Umbraco.Cms.Core.Services;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class ImgTagHelperTests
    {
        private TagHelperContext _tagHelperContext;

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

            var renderedSrc = output.Attributes["src"].Value.ToString();
            StringAssert.StartsWith(src + "?width=100&height=50", renderedSrc);
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

            var renderedSrc = output.Attributes["src"].Value.ToString();
            StringAssert.StartsWith("/some/child/images/logo.png?width=100&height=50", renderedSrc);
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

            var renderedSrc = output.Attributes["src"].Value.ToString();
            StringAssert.StartsWith("https://cdn.example.com/images/logo.png?width=100&height=50", renderedSrc);
        }

        private static ImgTagHelper CreateTagHelper(string requestPath)
        {
            var globalSettings = Options.Create(new OurUmbracoTagHelpersConfiguration());
            var mediaServiceMock = new Mock<IMediaService>();

            var tagHelper = new ImgTagHelper(globalSettings, mediaServiceMock.Object)
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
