#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Tests.Helpers;
using Umbraco.Cms.Web.Common.Security;

namespace Our.Umbraco.TagHelpers.Tests
{
    public class SurfaceControllerFormTagHelperTests
    {
        // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/93
        // Umbraco routes surface controllers by name without the Controller suffix
        [TestCase("ContactForm", "ContactForm")]
        [TestCase("ContactFormController", "ContactForm")]
        [TestCase("contactformcontroller", "contactform")]
        // ControllerController is routed as "Controller": with the suffix it's stripped once, and without it
        // the name is only the suffix itself, which must not be stripped to an empty name
        [TestCase("ControllerController", "Controller", Description = "ControllerController with its suffix")]
        [TestCase("Controller", "Controller", Description = "ControllerController, written without its suffix")]
        public void ControllerName_IsRoutedWithoutTheControllerSuffix(string ourController, string expectedRouteName)
        {
            var dataProtectionProvider = new EphemeralDataProtectionProvider();
            var tagHelper = new SurfaceControllerFormTagHelper(dataProtectionProvider)
            {
                ControllerName = ourController,
                ControllerAction = "Submit",
            };

            var output = TestContextHelpers.GetTagHelperOutput("form");
            tagHelper.Process(TestContextHelpers.GetTagHelperContext(), output);

            var ufprt = Regex.Match(output.PostContent.GetContent(), "value=\"([^\"]+)\"").Groups[1].Value;
            ClassicAssert.IsTrue(EncryptionHelper.DecryptAndValidateEncryptedRouteString(dataProtectionProvider, ufprt, out IDictionary<string, string?>? parts));
            ClassicAssert.AreEqual(expectedRouteName, parts![ufprtControllerKey]);
            ClassicAssert.AreEqual("Submit", parts[ufprtActionKey]);
        }

        // The keys Umbraco's EncryptionHelper uses for the controller and action in the ufprt value
        private const string ufprtControllerKey = "c";
        private const string ufprtActionKey = "a";
    }
}
