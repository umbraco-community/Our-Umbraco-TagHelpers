using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Umbraco.Cms.Core.Services;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Translates an Umbraco Dictionary Key for the current language
    /// or a fallback language and uses an element
    /// </summary>
    [HtmlTargetElement("our-dictionary", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class DictionaryTagHelper : TagHelper
    {
        private readonly IDictionaryItemService _dictionaryItemService;

        public DictionaryTagHelper(IDictionaryItemService dictionaryItemService)
        {
            _dictionaryItemService = dictionaryItemService;
        }

        /// <summary>
        /// The dictionary key to translate
        /// </summary>
        [HtmlAttributeName("key")]
        public string? Key { get; set; }

        /// <summary>
        /// An optional attribute to set a fallback language to use
        /// If the current language does not contain a translation for the key
        /// </summary>
        [HtmlAttributeName("fallback-lang")]
        public string? FallbackLang { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await base.ProcessAsync(context, output);

            output.TagName = ""; // Remove the outer tag of <our-dictionary>

            // Ensure we have a dictionary key to lookup
            if (string.IsNullOrEmpty(Key) == false)
            {
                // Get current culture
                var currentCulture = CultureInfo.CurrentCulture;

                // Ensure the Dictionary item/key even exist
                var translation = await _dictionaryItemService.GetAsync(Key);
                if (translation != null)
                {
                    // Try to see if we have a value set for the current culture/language
                    var langTranslation = translation.Translations.FirstOrDefault(x => x.LanguageIsoCode.Equals(currentCulture.Name, comparisonType: StringComparison.InvariantCultureIgnoreCase));
                    if (string.IsNullOrEmpty(langTranslation?.Value) == false)
                    {
                        // Only replace the HTML inside the <umb-dictionary> tag if we have a value
                        output.Content.SetHtmlContent(langTranslation.Value);
                    }

                    // If we can't find the current lang value - check if we have set an attribute to check for a fallback
                    else if (string.IsNullOrEmpty(FallbackLang) == false)
                    {
                        // Try & see if we have a value set for fallback lang
                        var fallbackLangTranslation = translation.Translations.FirstOrDefault(x => x.LanguageIsoCode.Equals(FallbackLang, comparisonType: StringComparison.InvariantCultureIgnoreCase));
                        if (string.IsNullOrEmpty(fallbackLangTranslation?.Value) == false)
                        {
                            // Only replace the HTML inside the <umb-dictionary> tag if we have a value for the fallback lang
                            output.Content.SetHtmlContent(fallbackLangTranslation.Value);
                        }
                    }
                }
            }
        }
    }
}
