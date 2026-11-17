using System;
using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Shows a number of bytes as a readable size, such as <c>1.1 MB</c>:
    /// <c>&lt;our-bytes value="1152302" /&gt;</c>, or the file size of a media item:
    /// <c>&lt;our-bytes media-item="@Model.Image" /&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Units are steps of 1024 (B, KB, MB, GB, TB, PB), and the number is formatted for the page's culture.
    /// Nothing is output when there's no value or it's negative.
    /// </remarks>
    [HtmlTargetElement("our-bytes", TagStructure = TagStructure.WithoutEndTag)]
    public class BytesTagHelper : TagHelper
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

        /// <summary>
        /// The number of bytes. When it's set, it's used instead of <see cref="MediaItem"/>.
        /// </summary>
        [HtmlAttributeName("value")]
        public long? Value { get; set; }

        /// <summary>
        /// A media item whose file size (the umbracoBytes property Umbraco sets on upload) is shown.
        /// </summary>
        [HtmlAttributeName("media-item")]
        public IPublishedContent? MediaItem { get; set; }

        /// <summary>
        /// The most decimals to show (default 1). Trailing zeros are left out, so exactly 1 MB is "1 MB".
        /// </summary>
        [HtmlAttributeName("decimals")]
        public int Decimals { get; set; } = 1;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // never render <our-bytes> itself

            var bytes = Value ?? MediaItemBytes();
            if (bytes is null or < 0)
            {
                output.SuppressOutput();
                return;
            }

            output.Content.SetContent(Format(bytes.Value, Math.Max(0, Decimals)));
        }

        // GetProperty is read through IPublishedElement, which declares it on every supported Umbraco version
        private long? MediaItemBytes() =>
            ((IPublishedElement?)MediaItem)?.GetProperty(Constants.Conventions.Media.Bytes)?.GetValue() switch
            {
                long value => value,
                int value => value,
                string value when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null,
            };

        private static string Format(long bytes, int decimals)
        {
            var unit = 0;
            var size = (double)bytes;
            while (size >= 1024 && unit < Units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            // 1023.99 KB rounds to "1024 KB": show it as 1 MB instead
            if (Math.Round(size, decimals) >= 1024 && unit < Units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            var format = decimals == 0 ? "0" : "0." + new string('#', decimals);
            return $"{size.ToString(format, CultureInfo.CurrentCulture)} {Units[unit]}";
        }
    }
}
