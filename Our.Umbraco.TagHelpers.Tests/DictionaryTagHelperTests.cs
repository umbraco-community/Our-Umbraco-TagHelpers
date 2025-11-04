using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.Dictionary;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class DictionaryTagHelperTests
    {
        private Mock<ILocalizationService> _mockLocalizationService;
        private DictionaryTagHelper _tagHelper;

        [SetUp]
        public void SetUp()
        {
            _mockLocalizationService = new Mock<ILocalizationService>();
            _tagHelper = new DictionaryTagHelper(_mockLocalizationService.Object);
        }

        [Test]
        public async Task Process_WithValidKeyAndTranslation_ReturnsTranslation()
        {
            // Arrange
            var key = "test.key";
            var expectedTranslation = "Test Translation";
            var culture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;

            var mockDictionaryItem = new Mock<IDictionaryItem>();
            var mockLanguage = new Mock<ILanguage>();
            mockLanguage.Setup(x => x.CultureInfo).Returns(culture);
            
            var mockTranslation = new Mock<IDictionaryTranslation>();
            mockTranslation.Setup(x => x.Language).Returns(mockLanguage.Object);
            mockTranslation.Setup(x => x.Value).Returns(expectedTranslation);
            
            mockDictionaryItem.Setup(x => x.Translations).Returns(new List<IDictionaryTranslation> { mockTranslation.Object });
            
            _mockLocalizationService.Setup(x => x.GetDictionaryItemByKey(key)).Returns(mockDictionaryItem.Object);

            var tagHelperContext = TestContextHelpers.GetTagHelperContext("dict-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-dictionary");

            _tagHelper.Key = key;

            // Act
            await Task.Run(() => _tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.AreEqual(expectedTranslation, content);
        }

        [Test]
        public async Task Process_WithNullKey_DoesNotModifyContent()
        {
            // Arrange
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("dict-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-dictionary");
            var originalContent = "Original content";
            tagHelperOutput.Content.SetHtmlContent(originalContent);

            _tagHelper.Key = null;

            // Act
            await Task.Run(() => _tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.AreEqual(originalContent, content);
        }

        [Test]
        public async Task Process_WithEmptyKey_DoesNotModifyContent()
        {
            // Arrange
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("dict-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-dictionary");
            var originalContent = "Original content";
            tagHelperOutput.Content.SetHtmlContent(originalContent);

            _tagHelper.Key = "";

            // Act
            await Task.Run(() => _tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.AreEqual(originalContent, content);
        }

        [Test]
        public async Task Process_WithNonExistentKey_DoesNotModifyContent()
        {
            // Arrange
            var key = "nonexistent.key";
            _mockLocalizationService.Setup(x => x.GetDictionaryItemByKey(key)).Returns((IDictionaryItem)null);

            var tagHelperContext = TestContextHelpers.GetTagHelperContext("dict-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("our-dictionary");
            var originalContent = "Original content";
            tagHelperOutput.Content.SetHtmlContent(originalContent);

            _tagHelper.Key = key;

            // Act
            await Task.Run(() => _tagHelper.Process(tagHelperContext, tagHelperOutput));

            var content = tagHelperOutput.Content.GetContent();

            // Assert
            Assert.AreEqual("", tagHelperOutput.TagName); // Tag name should be removed
            Assert.AreEqual(originalContent, content);
        }
    }
}