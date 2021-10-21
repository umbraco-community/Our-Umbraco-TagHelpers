using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// This allows you to inline an SVG file into the DOM 
    /// from a file on disk or an Umbraco Media Item
    /// </summary>
    [HtmlTargetElement("our-svg")]
    public class InlineSvgTagHelper : TagHelper
    {
        private MediaFileManager _mediaFileManager;
        private IWebHostEnvironment _webHostEnvironment;

        public InlineSvgTagHelper(MediaFileManager mediaFileManager, IWebHostEnvironment webHostEnvironment)
        {
            _mediaFileManager = mediaFileManager;
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// A filepath to a SVG on disk such as /assets/icon.svg
        /// NOTE: You cannot use this in conjuction with the media-item attribute
        /// </summary>
        [HtmlAttributeName("src")]
        public string FileSource { get; set; }

        /// <summary>
        /// An IPublishedContent Umbraco Media Item that has an .svg file extension
        /// NOTE: You cannot use this in conjuction with the src attribute
        /// </summary>
        [HtmlAttributeName("media-item")]
        public IPublishedContent MediaItem { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Can only use media-item OR src
            // Can't use both properties together
            if(string.IsNullOrWhiteSpace(FileSource) == false && MediaItem is not null)
            {
                // KABOOM !
                // Can't decide which property to use to render inline SVG
                // So just render nothing...
                output.SuppressOutput();
                return;
            }

            // SVG fileContents to render to DOM
            var fileContents = string.Empty;

            if(MediaItem is not null)
            {
                // Check Umbraco Media Item that is picked/used
                // has a file that uses a .svg file extension
                var mediaItemPath = MediaItem.Url();
                if(mediaItemPath.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    output.SuppressOutput();
                    return;
                }

                // Ensure the file actually exists on disk, Azure blob provider or ...
                // Anywhere else defined by IFileSystem to fetch & store files
                if (_mediaFileManager.FileSystem.FileExists(mediaItemPath) == false)
                {
                    output.SuppressOutput();
                    return;
                }

                // Read its contents (get its stream)
                var fileStream = _mediaFileManager.FileSystem.OpenFile(mediaItemPath);
                using var reader = new StreamReader(fileStream);
                fileContents = reader.ReadToEnd();
            }
            else if(string.IsNullOrWhiteSpace(FileSource) == false)
            {
                // Check string src filepath ends with .svg
                if (FileSource.EndsWith(".svg", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    output.SuppressOutput();
                    return;
                }

                // Get file from wwwRoot using a path such as
                // /assets/logo/my-logo.svg as opposed to wwwRoot/assets/logo/my-logo.svg
                // Can we or should we support ~ in paths at root?
                var webRoot = _webHostEnvironment.WebRootFileProvider;
                var file = webRoot.GetFileInfo(FileSource);

                // Ensure file exists in wwwroot path
                if(file.Exists == false)
                {
                    output.SuppressOutput();
                    return;
                }

                fileContents = File.ReadAllText(file.PhysicalPath);
            }


            // Sanatize SVG (Is there anything in Umbraco to reuse)
            // https://stackoverflow.com/questions/65247336/is-there-anyway-to-sanitize-svg-file-in-c-any-libraries-anything/65375485#65375485
            var cleanedFileContents = Regex.Replace(fileContents,
                @"<script.*?script>",
                @"",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            cleanedFileContents = Regex.Replace(cleanedFileContents,
                @"javascript:",
                @"syntax:error:",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove the src attribute or media-item from the <svg>
            output.Attributes.RemoveAll("src");
            output.Attributes.RemoveAll("media-item");

            output.TagName = null; // Remove <our-svg>
            output.Content.SetHtmlContent(cleanedFileContents);
       }
    }
}
