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
        [Test]
        public async Task Given_True_Value_Return_Contents()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            tagHelperOutput.Content.SetContent(childContent);

            var tagHelper = new IncludeIfTagHelper();
            tagHelper.Predicate = true;

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            var content = tagHelperOutput.Content.GetContent();
            Console.WriteLine("Value:" + content);

            // Assert
            Assert.AreEqual(childContent, tagHelperOutput.Content.GetContent());
        }

        [Test]
        public async Task Given_False_Value_Return_Empty()
        {
            // Arrange
            var id = "unique-id";
            var childContent = "original-child-content";
            var tagHelperContext = GetTagHelperContext(id);
            var tagHelperOutput = GetTagHelperOutput(
                attributes: new TagHelperAttributeList(),
                childContent: childContent);
            tagHelperOutput.Content.SetContent(childContent);

            var tagHelper = new IncludeIfTagHelper();
            tagHelper.Predicate = false;

            // Act
            await tagHelper.ProcessAsync(tagHelperContext, tagHelperOutput);

            var content = tagHelperOutput.Content.GetContent();
            Console.WriteLine("Value:" + content);

            // Assert
            Assert.AreEqual(string.Empty, tagHelperOutput.Content.GetContent());
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