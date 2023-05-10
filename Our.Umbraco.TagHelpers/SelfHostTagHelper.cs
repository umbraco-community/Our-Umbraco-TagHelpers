using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Services;
using System.Threading.Tasks;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Downloads the specified file (in href or src) to wwwroot and changes the link to local.
    /// </summary>
    [HtmlTargetElement("*", Attributes = "our-self-host")]
    public class SelfHostTagHelper : TagHelper
    {
        private readonly ISelfHostService _selfHostService;

        public SelfHostTagHelper(ISelfHostService selfHostService)
        {
            _selfHostService = selfHostService;
        }

        [HtmlAttributeName("folder")]
        public string? FolderName { get; set; }
        [HtmlAttributeName("src")]
        public string? SrcAttribute { get; set; }
        [HtmlAttributeName("href")]
        public string? HrefAttribute { get; set; }
        [HtmlAttributeName("ext")]
        public string? Extension { get; set; }
        public string Url => SrcAttribute.IfNullOrWhiteSpace(HrefAttribute);

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var url = (Url.StartsWith("//") ? $"https:{Url}" : Url);
            var selfHostedFile = await _selfHostService.SelfHostFile(url, FolderName, Extension);

            if (SrcAttribute.IsNullOrWhiteSpace() == false)
            {
                output.Attributes.SetAttribute("data-original-src", SrcAttribute);
                output.Attributes.SetAttribute("src", selfHostedFile.Url);
            }
            else if (HrefAttribute.IsNullOrWhiteSpace() == false)
            {
                output.Attributes.SetAttribute("data-original-href", HrefAttribute);
                output.Attributes.SetAttribute("href", selfHostedFile.Url);
            }

            output.Attributes.Remove(new TagHelperAttribute("our-self-host"));
            output.Attributes.Remove(new TagHelperAttribute("folder"));
            output.Attributes.Remove(new TagHelperAttribute("ext"));
        }


    }
}
