using Microsoft.AspNetCore.Razor.TagHelpers;
using NUnit.Framework;
using System.Threading.Tasks;
using Our.Umbraco.TagHelpers.Tests.Helpers;

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
            var tagHelperContext = TestContextHelpers.GetTagHelperContext(id);
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput(
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
    }
}