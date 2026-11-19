#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Configuration;
using Umbraco.Cms.Core.Cache;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/115
    // unique-ids="true" prefixes every id in the inlined SVG, and every reference to it, per tag
    public class InlineSvgUniqueIdsTests
    {
        // Every kind of reference: url(#) in attributes, style attributes and <style>, href and xlink:href,
        // aria-labelledby, and a #id selector; plus a colour and a url(#) to an id the file doesn't define
        private const string Svg =
            "<svg aria-labelledby=\"title\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">" +
            "<title id=\"title\">Logo</title>" +
            "<style>#shape { stroke: url(#grad); } .a { fill: url('#grad'); }</style>" +
            "<defs><linearGradient id=\"grad\"></linearGradient><clipPath id=\"clip\"></clipPath></defs>" +
            "<rect id=\"shape\" fill=\"url(#grad)\" clip-path=\"url(#clip)\" style=\"stroke: url(&quot;#grad&quot;)\"></rect>" +
            "<use href=\"#shape\"></use><use xlink:href=\"#shape\"></use>" +
            "<circle fill=\"#fff\" stroke=\"url(#elsewhere)\"></circle>" +
            "</svg>";

        private static string Render(bool? uniqueIds, bool appSettings = false, bool ignoreAppSettings = false, bool cache = false,
            AppCaches? appCaches = null, TagHelperAttributeList? otherAttributes = null)
        {
            var fileProvider = new Mock<IFileProvider>();
            fileProvider.Setup(p => p.GetFileInfo(It.IsAny<string>()))
                .Returns(() => Mock.Of<IFileInfo>(f => f.Exists && f.CreateReadStream() == new MemoryStream(Encoding.UTF8.GetBytes(Svg))));
            var settings = new OurUmbracoTagHelpersConfiguration();
            settings.OurSVG.UniqueIds = appSettings;

            var tagHelper = new InlineSvgTagHelper(null!, Mock.Of<IWebHostEnvironment>(e => e.WebRootFileProvider == fileProvider.Object), null!,
                Options.Create(settings), appCaches ?? AppCaches.Disabled, Mock.Of<ILogger<InlineSvgTagHelper>>())
            {
                FileSource = "/logo.svg",
                IgnoreAppSettings = ignoreAppSettings,
            };
            if (uniqueIds is bool value) tagHelper.UniqueIds = value; // like Razor, only when the attribute is on the tag
            if (cache) tagHelper.Cache = true;

            var attributes = otherAttributes ?? new TagHelperAttributeList();
            var output = new TagHelperOutput("our-svg", attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            tagHelper.Process(new TagHelperContext(attributes, new Dictionary<object, object>(), "test"), output);
            return output.Content.GetContent();
        }

        private static string[] Ids(string svg) => Regex.Matches(svg, "\\sid=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();

        [Test]
        public void IdsAndEveryReference_GetTheSamePrefix()
        {
            var svg = Render(uniqueIds: true);
            var titleId = Ids(svg).Single(id => id.EndsWith("title"));
            var prefix = titleId[..^"title".Length];

            StringAssert.IsMatch("^[a-z][a-z0-9]*-$", prefix);
            CollectionAssert.AreEquivalent(new[] { "title", "grad", "clip", "shape" }.Select(id => prefix + id), Ids(svg));
            StringAssert.Contains($"aria-labelledby=\"{prefix}title\"", svg);
            StringAssert.Contains($"#{prefix}shape {{ stroke: url(#{prefix}grad); }} .a {{ fill: url('#{prefix}grad'); }}", svg);
            StringAssert.Contains($"fill=\"url(#{prefix}grad)\" clip-path=\"url(#{prefix}clip)\" style=\"stroke: url(&quot;#{prefix}grad&quot;)\"", svg);
            StringAssert.Contains($"<use href=\"#{prefix}shape\"></use><use xlink:href=\"#{prefix}shape\"></use>", svg);
        }

        [Test]
        public void ColoursAndReferencesToOtherIds_AreLeftAlone()
        {
            StringAssert.Contains("<circle fill=\"#fff\" stroke=\"url(#elsewhere)\"></circle>", Render(uniqueIds: true));
        }

        [Test]
        public void EachTag_GetsItsOwnPrefix_EvenFromTheCache()
        {
            var appCaches = new AppCaches(new ObjectCacheAppCache(), NoAppCache.Instance, new IsolatedCaches(_ => NoAppCache.Instance));

            var first = Ids(Render(uniqueIds: true, cache: true, appCaches: appCaches));
            var second = Ids(Render(uniqueIds: true, cache: true, appCaches: appCaches));

            CollectionAssert.IsEmpty(first.Intersect(second));
        }

        [Test]
        public void AnIdOnTheOurSvgTag_IsKeptAsWritten()
        {
            var svg = Render(uniqueIds: true, otherAttributes: new TagHelperAttributeList { { "id", "site-logo" } });
            StringAssert.StartsWith("<svg aria-labelledby=", svg);
            CollectionAssert.Contains(Ids(svg), "site-logo");
        }

        // (appsettings UniqueIds, unique-ids attribute or null when not set, ignore-appsettings, expected unique)
        [TestCase(false, null, false, false)]
        [TestCase(true, null, false, true)]
        [TestCase(true, null, true, false)]
        [TestCase(false, true, false, true)]
        [TestCase(true, false, false, false)]
        [TestCase(false, true, true, true)]
        public void AttributeAndAppSettings(bool appSettings, bool? attribute, bool ignoreAppSettings, bool expectUnique)
        {
            var ids = Ids(Render(attribute, appSettings, ignoreAppSettings));
            ClassicAssert.AreEqual(!expectUnique, ids.Contains("grad"));
        }
    }
}
