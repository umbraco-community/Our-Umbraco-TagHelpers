#nullable enable
using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.TagHelpers.Tests
{
    // our-fallback must output a property's value the way @Model.Value(...) in Razor does:
    // plain values HTML-encoded, values that are already HTML (rich text) as HTML
    public class OurFallbackTagHelperTests
    {
        private IServiceProvider? _originalServiceProvider;

        [SetUp]
        public void SetUp()
        {
            _originalServiceProvider = StaticServiceProvider.Instance;
            StaticServiceProvider.Instance = new MockingServiceProvider(new ServiceCollection()
                .AddSingleton(Mock.Of<IVariationContextAccessor>(a => a.VariationContext == new VariationContext(null, null)))
                .BuildServiceProvider());
        }

        // Umbraco's Value() extension resolves several services statically (which ones varies by Umbraco
        // version); none matter here, so any interface that isn't registered gets a mock
        private sealed class MockingServiceProvider(IServiceProvider services) : IServiceProvider
        {
            public object? GetService(Type serviceType) =>
                services.GetService(serviceType)
                ?? (serviceType.IsInterface ? ((Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(serviceType))!).Object : null);
        }

        [TearDown]
        public void TearDown() => StaticServiceProvider.Instance = _originalServiceProvider!;

        [TestCase("Plain header", "Plain header")]
        [TestCase("<em>Not emphasised</em>", "&lt;em&gt;Not emphasised&lt;/em&gt;", Description = "markup in a plain-text property is shown as text")]
        [TestCase("<script>alert(1)</script>", "&lt;script&gt;alert(1)&lt;/script&gt;", Description = "a script in a plain-text property doesn't run")]
        public void PlainValue_IsHtmlEncoded(string value, string expected)
        {
            ClassicAssert.AreEqual(expected, Render(value));
        }

        [Test]
        public void RichText_IsRenderedAsHtml()
        {
            ClassicAssert.AreEqual("<p><strong>Rich</strong> text</p>", Render(new HtmlEncodedString("<p><strong>Rich</strong> text</p>")));
        }

        [Test]
        public void HtmlContent_IsRenderedAsHtml()
        {
            ClassicAssert.AreEqual("<b>Html</b>", Render(new HtmlString("<b>Html</b>")));
        }

        [Test]
        public void NoValue_KeepsTheContentInsideTheTag()
        {
            ClassicAssert.AreEqual("Fallback content", Render(null, childContent: "Fallback content"));
        }

        private static string Render(object? value, string childContent = "Fallback content")
        {
            var property = new Mock<IPublishedProperty>();
            property.Setup(p => p.HasValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(value is not null);
            property.Setup(p => p.GetValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(value);
            var content = new Mock<IPublishedContent>();
            content.Setup(c => c.GetProperty("Header")).Returns(property.Object);

            // As Razor builds it for property="Header": a simple (string) property, not a complex type or collection
            var metadataProvider = new EmptyModelMetadataProvider();
            var modelExplorer = metadataProvider.GetModelExplorerForType(typeof(string), value?.ToString());
            var tagHelper = new OurFallbackTagHelper
            {
                ModelProperty = new ModelExpression("Header", modelExplorer),
                ViewContext = new ViewContext { ViewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary()) { Model = content.Object } },
            };

            var output = TestContextHelpers.GetTagHelperOutput("our-fallback", childContent: childContent);
            output.Content.SetContent(childContent);
            tagHelper.Process(TestContextHelpers.GetTagHelperContext(), output);
            return output.Content.GetContent();
        }
    }
}
