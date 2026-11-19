#nullable enable
using System.Globalization;
using System.Threading;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/14
    // <our-bytes value="1152302" /> renders 1.1 MB
    public class BytesTagHelperTests
    {
        private static string? Render(BytesTagHelper tagHelper, string culture = "en-US")
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            try
            {
                var output = TestContextHelpers.GetTagHelperOutput("our-bytes");
                tagHelper.Process(TestContextHelpers.GetTagHelperContext(), output);
                ClassicAssert.IsNull(output.TagName, "the <our-bytes> element itself is never rendered");
                return output.IsContentModified && !output.Content.IsEmptyOrWhiteSpace ? output.Content.GetContent() : null;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestCase(0L, "0 B")]
        [TestCase(512L, "512 B")]
        [TestCase(1023L, "1023 B")]
        [TestCase(1024L, "1 KB")]
        [TestCase(1536L, "1.5 KB")]
        [TestCase(1152302L, "1.1 MB")]
        [TestCase(1073741824L, "1 GB")]
        [TestCase(5497558138880L, "5 TB")]
        [TestCase(1048575L, "1 MB")] // 1023.999 KB rounds up to the next unit, not "1024 KB"
        public void Value_IsShownInTheLargestUnit(long bytes, string expected)
        {
            ClassicAssert.AreEqual(expected, Render(new BytesTagHelper { Value = bytes }));
        }

        [TestCase(2, 1234567L, "1.18 MB")]
        [TestCase(0, 1536L, "2 KB")]
        public void Decimals_SetsHowManyDecimalsAreShown(int decimals, long bytes, string expected)
        {
            ClassicAssert.AreEqual(expected, Render(new BytesTagHelper { Value = bytes, Decimals = decimals }));
        }

        [Test]
        public void Number_UsesThePageCulture()
        {
            ClassicAssert.AreEqual("1,5 KB", Render(new BytesTagHelper { Value = 1536 }, "da"));
        }

        [TestCase(null)]
        [TestCase(-1L)]
        public void NoValue_OutputsNothing(long? bytes)
        {
            ClassicAssert.IsNull(Render(new BytesTagHelper { Value = bytes }));
        }

        private static IPublishedContent Media(object? bytes)
        {
            var property = Mock.Of<IPublishedProperty>(p => p.GetValue(It.IsAny<string?>(), It.IsAny<string?>()) == bytes);
            var media = new Mock<IPublishedContent>();
            media.Setup(m => m.GetProperty("umbracoBytes")).Returns(bytes is null ? null : property);
            return media.Object;
        }

        [TestCase(2048L)]
        [TestCase(2048)]
        [TestCase("2048")]
        public void MediaItem_UsesItsFileSize(object bytes)
        {
            ClassicAssert.AreEqual("2 KB", Render(new BytesTagHelper { MediaItem = Media(bytes) }));
        }

        [Test]
        public void MediaItem_WithoutAFileSize_OutputsNothing()
        {
            ClassicAssert.IsNull(Render(new BytesTagHelper { MediaItem = Media(null) }));
        }

        [Test]
        public void Value_WinsOverMediaItem()
        {
            ClassicAssert.AreEqual("1 KB", Render(new BytesTagHelper { Value = 1024, MediaItem = Media(2048L) }));
        }
    }
}
