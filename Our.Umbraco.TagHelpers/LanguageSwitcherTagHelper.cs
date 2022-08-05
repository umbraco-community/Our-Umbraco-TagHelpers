using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Stubble.Core.Builders;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// Use <our-lang-switcher> for a list of Domain/Culture links
    /// HTML markup inside the element can be used to controll the HTML 
    /// output by using Mustache templating language
    /// </summary>
    [HtmlTargetElement("our-lang-switcher")]
    public class LanguageSwitcherTagHelper : TagHelper
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        static string DefaultMustacheTemplate = @"
    <ul class='lang-switcher'>
        {{#Languages}}
            <li>
                <a href='{{Url}}' lang='{{Culture}}' hreflang='{{Culture}}' class='{{#IsCurrentLang}}selected{{/IsCurrentLang}}'>
                    {{Name}}
                </a>
            </li>
        {{/Languages}}
    </ul>";

        public LanguageSwitcherTagHelper(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = ""; // Remove the outer <umb-lang-switcher> tag

            // Attempt to get context
            if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
            {
                // Current Culture of the page
                var currentCulture = umbracoContext.PublishedRequest.Culture;

                // Current Page/Content node used
                var currentNode = umbracoContext.PublishedRequest.PublishedContent;

                // All domains in a site
                // NOTE: A culture/language could have multiple domains assigned (So use the first we find)
                var allDomains = umbracoContext.Domains.GetAll(false);
                var allDomainsFiltered = allDomains?.GroupBy(d => d.Culture).Select(g => g.First()).ToList();

                if (allDomainsFiltered?.Count() > 0)
                {
                    // List of languages/model to use with
                    var data = new Dictionary<string, object>();
                    var languages = new List<LanguageSwitcherLang>();

                    foreach (var domain in allDomainsFiltered)
                    {
                        // Will get the language such as English or Dansk without the (United States) country prefixed
                        var culture = new CultureInfo(domain.Culture);
                        var languageName = culture.IsNeutralCulture ? culture.NativeName : culture.Parent.NativeName;

                        var lang = new LanguageSwitcherLang
                        {
                            Name = languageName,
                            Culture = domain.Culture,
                            Url = domain.Name,
                            IsCurrentLang = domain.Culture == currentCulture
                        };

                        languages.Add(lang);                       
                    }

                    data.Add("Languages", languages);

                    // Get HTML content inside taghelper
                    // This content inside is a HTML Mustache template
                    // Will fallback to a default template if none is set
                    var content = await output.GetChildContentAsync();
                    var mustacheTemplate = string.IsNullOrWhiteSpace(content.GetContent()) == false ? content.GetContent() : DefaultMustacheTemplate;

                    var stubble = new StubbleBuilder().Build();
                    var rendered = stubble.Render(mustacheTemplate, data);

                    output.Content.SetHtmlContent(rendered);
                }
                else
                {
                    // No domains so render nothing
                    output.SuppressOutput();
                }
            }
        }
    }

    public class LanguageSwitcherLang
    {
        public string? Culture { get; set; }

        public string? Name { get; set; }

        public string? Url { get; set; }

        public bool IsCurrentLang { get; set; }
    }
}
