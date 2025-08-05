using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using System;
using System.Collections.Generic;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class SurfaceControllerFormTagHelperTests
    {
        [Test]
        public void Constructor_WithValidParameters_InitializesProperties()
        {
            // Arrange
            var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();

            // Act
            var tagHelper = new SurfaceControllerFormTagHelper(mockDataProtectionProvider.Object);

            // Assert
            Assert.IsNotNull(tagHelper);
            Assert.AreEqual("", tagHelper.Area);
            Assert.IsNull(tagHelper.ControllerAction);
            Assert.IsNull(tagHelper.ControllerName);
            Assert.IsNotNull(tagHelper.RouteValues);
        }

        [Test]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            var tagHelper = new SurfaceControllerFormTagHelper(mockDataProtectionProvider.Object);

            // Act
            tagHelper.ControllerAction = "TestAction";
            tagHelper.ControllerName = "TestController";
            tagHelper.Area = "TestArea";
            
            var routeValues = new Dictionary<string, string> { { "id", "123" } };
            tagHelper.RouteValues = routeValues;

            // Assert
            Assert.AreEqual("TestAction", tagHelper.ControllerAction);
            Assert.AreEqual("TestController", tagHelper.ControllerName);
            Assert.AreEqual("TestArea", tagHelper.Area);
            Assert.AreEqual(routeValues, tagHelper.RouteValues);
        }

        [Test]
        public void Process_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            var tagHelper = new SurfaceControllerFormTagHelper(mockDataProtectionProvider.Object);
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("form");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tagHelper.Process(null, tagHelperOutput));
        }

        [Test]
        public void Process_WithNullOutput_ThrowsArgumentNullException()
        {
            // Arrange
            var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            var tagHelper = new SurfaceControllerFormTagHelper(mockDataProtectionProvider.Object);
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("form-id");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => tagHelper.Process(tagHelperContext, null));
        }

        [Test]
        public void Process_WithEmptyControllerName_ReturnsEarly()
        {
            // Arrange
            var mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            var tagHelper = new SurfaceControllerFormTagHelper(mockDataProtectionProvider.Object);
            var tagHelperContext = TestContextHelpers.GetTagHelperContext("form-id");
            var tagHelperOutput = TestContextHelpers.GetTagHelperOutput("form");

            tagHelper.ControllerName = "";
            tagHelper.ControllerAction = "TestAction";

            // Act - Should return early without doing anything
            tagHelper.Process(tagHelperContext, tagHelperOutput);

            // Assert - Since it returns early, PostContent should not be modified
            // This is a basic test to ensure the method doesn't throw
            Assert.DoesNotThrow(() => tagHelper.Process(tagHelperContext, tagHelperOutput));
        }
    }
}