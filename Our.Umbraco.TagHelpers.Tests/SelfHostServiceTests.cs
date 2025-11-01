using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Our.Umbraco.TagHelpers.Configuration;
using Our.Umbraco.TagHelpers.Services;
using System;
using NUnit.Framework.Legacy;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class SelfHostServiceTests
    {
        private Mock<IProfilingLogger> _loggerMock;
        private Mock<IAppPolicyCache> _runtimeCacheMock;
        private Mock<IWebHostEnvironment> _hostingEnvironmentMock;
        private Mock<IOptions<OurUmbracoTagHelpersConfiguration>> _globalSettingsMock;
        private SelfHostService _service;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<IProfilingLogger>();
            _runtimeCacheMock = new Mock<IAppPolicyCache>();
            _hostingEnvironmentMock = new Mock<IWebHostEnvironment>();
            _globalSettingsMock = new Mock<IOptions<OurUmbracoTagHelpersConfiguration>>();
            _globalSettingsMock.SetupGet(x => x.Value).Returns(new OurUmbracoTagHelpersConfiguration
            {
                OurSelfHost = new SelfHostTagHelperConfiguration
                {
                    RootFolder = "/self-hosted/"
                }
            });
            _service = new SelfHostService(
                _loggerMock.Object,
                _runtimeCacheMock.Object,
                _hostingEnvironmentMock.Object,
                _globalSettingsMock.Object
            );
        }

        [Test]
        public void GetRemoteFolderPath_GivenUriWithMoreThanTwoSegments_ReturnsFolderPath()
        {
            // Arrange
            var uri = new Uri("http://www.example.com/folder/subfolder/file.jpg");
            var expectedFolderPath = "/folder/subfolder";

            // Act
            var result = _service.GetRemoteFolderPath(uri);

            // Assert
            ClassicAssert.AreEqual(expectedFolderPath, result);
        }

        [Test]
        public void GetRemoteFolderPath_GivenUriWithLessThanTwoSegments_ReturnsEmptyString()
        {
            // Arrange
            var uri = new Uri("http://www.example.com/");
            var expectedFolderPath = string.Empty;

            // Act
            var result = _service.GetRemoteFolderPath(uri);

            // Assert
            ClassicAssert.AreEqual(expectedFolderPath, result);
        }
    }
}