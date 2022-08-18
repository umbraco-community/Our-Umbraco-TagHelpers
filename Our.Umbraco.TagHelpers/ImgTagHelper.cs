using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// This allows you to set some PageSpeed Insights friendly optimisations to your img tags
    /// including lazy loading placeholders, dynamic alt text, aspect ratios 
    /// & image quality (for non-media items, but still on the file system)
    /// </summary>
    [HtmlTargetElement("our-img")]
    public class ImgTagHelper : TagHelper
    {
        private IWebHostEnvironment _webHostEnvironment;

        public ImgTagHelper(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HtmlAttributeName("src")]
        public string FileSource { get; set; }

        [HtmlAttributeName("lazy-src")]
        public string ImgLazySrc { get; set; }

        [HtmlAttributeName("alt")]
        public string ImgAlt { get; set; }

        [HtmlAttributeName("width")]
        public string ImgWidth { get; set; }

        [HtmlAttributeName("height")]
        public string ImgHeight { get; set; }

        [HtmlAttributeName("style")]
        public string ImgStyle { get; set; }

        [HtmlAttributeName("quality")]
        public int ImgQuality { get; set; }

        [HtmlAttributeName("class")]
        public string ImgClass { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var cssClasses = new List<string>();

            if (!string.IsNullOrEmpty(ImgClass))
            {
                cssClasses.Add(ImgClass);
            }

            if (string.IsNullOrWhiteSpace(ImgWidth) || string.IsNullOrWhiteSpace(ImgHeight))
            {
                if (!string.IsNullOrEmpty(ImgLazySrc))
                {
                    output.Attributes.Add("src", ImgLazySrc);
                }
                else
                {
                    output.Attributes.Add("src", FileSource);
                }

                if (cssClasses.Count > 0)
                {
                    output.Attributes.Add("class", string.Join(" ", cssClasses));
                }

                output.TagName = "img";
                return;
            }

            if (string.IsNullOrWhiteSpace(ImgAlt))
            {
                output.Attributes.Add("alt", GetImageAltText(FileSource));
            }

            var width = int.Parse(ImgWidth);
            var height = int.Parse(ImgHeight);
            var style = ImgStyle;

            if (width > 0 && height > 0)
            {
                var aspectRatio = $"aspect-ratio: {width} / {height};";
                style = style.Trim().TrimEnd(';');
                if (!string.IsNullOrEmpty(ImgStyle))
                {
                    style += ";";
                    output.Attributes.RemoveAll("style");
                }
                style += aspectRatio;
                output.Attributes.Add("style", style);
                output.Attributes.Add("width", width);
                output.Attributes.Add("height", height);
            }


            var quality = ImgQuality > 0 ? ImgQuality : 100;

            if (!string.IsNullOrEmpty(ImgLazySrc))
            {
                output.Attributes.Add("data-src", ImgLazySrc);
                output.Attributes.Add("src", $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}'%3E%3C/svg%3E");
                cssClasses.Add("lazyload");
            }
            else
            {
                output.Attributes.Add("src", FileSource);
            }

            if (cssClasses.Count > 0)
            {
                output.Attributes.Add("class", string.Join(" ", cssClasses));
            }

            output.TagName = "img";
        }

        public static string GetImageAltText(string url)
        {
            try
            {
                var SomeBaseUri = new Uri("http://canbeanything");
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    uri = new Uri(SomeBaseUri, url);

                return Path.GetFileNameWithoutExtension(uri.LocalPath);

            }
            catch (Exception ex)
            {
                
            }

            return "";
        }
    }
}
