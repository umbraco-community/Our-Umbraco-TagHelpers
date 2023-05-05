using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Tests.Helpers
{
    internal static class TestContextHelpers
    {
        public static TagHelperContext GetTagHelperContext(string id = "testid")
        {
            return new TagHelperContext(
                tagName: "p",
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
        }

        public static TagHelperOutput GetTagHelperOutput(
            string tagName = "p",
            TagHelperAttributeList attributes = null,
            string childContent = "some child content")
        {
            attributes ??= new TagHelperAttributeList();

            return new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    var content = tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult(content);
                });
        }
    }
}
