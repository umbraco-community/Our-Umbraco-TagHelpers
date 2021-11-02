using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Our.Umbraco.TagHelpers;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class IncludeIfTagHelperTests
    {
        [TestCase(true, "original-child-content", "original-child-content")]
        [TestCase(false, "original-child-content", "")]
        public async Task Given_Predicate_Return_Contents_Or_Empty(bool predicate, string childContent, string expected)
        {
            // Arrange
            var id = "unique-id";
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            tagHelperOutput.Content.SetContent(childContent);

            var tagHelper = new IncludeIfTagHelper { Predicate = predicate };

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual(expected, content);
        }

        private static TagHelperContext GetTagHelperContext(string id = "testid")
        {
            return new TagHelperContext(
                tagName: "p",
                allAttributes: new TagHelperAttributeList(),
                items: new Dictionary<object, object>(),
                uniqueId: id);
        }

        private static TagHelperOutput GetTagHelperOutput(
            string tagName = "p",
            TagHelperAttributeList attributes = null,
            string childContent = "some child content")
        {
            attributes = attributes ?? new TagHelperAttributeList { { "attr", "value" } };

            return new TagHelperOutput(
                tagName,
                attributes,
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    var content = tagHelperContent.SetHtmlContent(childContent);
                    return Task.FromResult<TagHelperContent>(content);
                });
        }
    }
}