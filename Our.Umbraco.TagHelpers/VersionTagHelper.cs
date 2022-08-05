using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Linq;
using System.Reflection;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Used to print out the current version of the Assembly Name
    /// Only Assemblies loaded into the Current AppDomain can be looked up
    /// </summary>
    [HtmlTargetElement("our-version")]
    public class VersionTagHelper : TagHelper
    {
        /// <summary>
        /// Name of assembly to lookup minus the .dll extension
        /// 'Our.Umbraco.TagHelpers'
        /// </summary>
        [HtmlAttributeName("assembly")]
        public string AssemblyName { get; set; } = string.Empty;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "";

            // If no Assembly name set on attribute
            // Then use the 
            if (string.IsNullOrEmpty(AssemblyName))
            {
                var siteAssembly = Assembly.GetEntryAssembly();
                var version = siteAssembly?.GetName()?.Version;
                output.Content.SetHtmlContent(version?.ToString());
                return;
            }

            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var findAssembly = allAssemblies.SingleOrDefault(x => x.GetName().Name == AssemblyName);
            if(findAssembly == null)
            {
                output.SuppressOutput();
                return;
            }
            output.Content.SetHtmlContent(findAssembly.GetName()?.Version?.ToString());
        }
    }
}
