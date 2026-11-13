using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Extensions;
using Our.Umbraco.TagHelpers.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// If the user viewing the front end is logged in as an umbraco user
    /// then an edit link will display on the front end of the site. This will
    /// take you to the umbraco backoffice to edit the current page.
    /// </summary>
    [HtmlTargetElement("our-edit-link")]
    public class EditLinkTagHelper : TagHelper
    {
        private readonly IBackofficeUserAccessor _backofficeUserAccessor;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IUserService _userService;

        public EditLinkTagHelper(
            IBackofficeUserAccessor backofficeUserAccessor,
            IUmbracoContextAccessor umbracoContextAccessor,
            IUserService userService)
        {
            _backofficeUserAccessor = backofficeUserAccessor;
            _umbracoContextAccessor = umbracoContextAccessor;
            _userService = userService;
        }

        /// <summary>
        /// A string of the GUID/Key of the content item to link to
        /// If not set it will use the current page's key
        /// </summary>
        [HtmlAttributeName("content-key")]
        public string ContentKey { get; set; } = string.Empty;

        /// <summary>
        /// A boolean to say whether or not you would like to use the default styling.
        /// </summary>
        [HtmlAttributeName("use-default-styles")]
        public bool UseDefaultStyles { get; set; } = false;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Turn <our-edit-link> into an <a> tag
            output.TagName = "a";

            // An outer wrapper div if we use inbuilt styling
            var outerDiv = new TagBuilder("div");

            // Check if the user is logged in to the backoffice
            // and they have access to the content section
            if (await IsAllowedToSeeEditLinkAsync(_backofficeUserAccessor?.BackofficeUser))
            {
                // Try & get Umbraco Current Node Key (Only do this if ContentKey has NOT been set)
                if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) && string.IsNullOrWhiteSpace(ContentKey))
                {
                    if (umbracoContext.PublishedRequest?.PublishedContent is not null)
                    {
                        ContentKey = umbracoContext.PublishedRequest.PublishedContent.Key.ToString();
                    }
                }

                // Backoffice URL to content item
                // /umbraco/section/content/workspace/document/edit/b0c59e4c-158c-4fd4-9beb-ebe907693f1c
                var editLinkUrl = $"/umbraco/section/content/workspace/document/edit/{ContentKey}";

                if (UseDefaultStyles)
                {
                    // Wrap the <a> in a <div>
                    // Render the outer div with some inline styles
                    outerDiv.Attributes.Add("style", GetOuterElementStyles());
                    output.PreElement.AppendHtml(outerDiv.RenderStartTag());
                }

                // Set the link on the <a> tag
                output.Attributes.SetAttribute("href", editLinkUrl);

                if (UseDefaultStyles)
                {
                    output.Attributes.SetAttribute("style", GetLinkStyles());
                }

                if (UseDefaultStyles)
                {
                    //Add the closing outer div
                    output.PostElement.AppendHtml(outerDiv.RenderEndTag());
                }

                return;
            }
            else
            {
                output.SuppressOutput();
                return;
            }
        }

        /// <summary>
        /// The user must be logged into the backoffice and have access to the content section.
        /// Section access is read from the user record rather than a cookie claim, as the
        /// allowed-applications claim type was removed from Umbraco in v19.
        /// </summary>
        private async Task<bool> IsAllowedToSeeEditLinkAsync(ClaimsIdentity? identity)
        {
            if (identity is null || !identity.IsLoggedIntoUmbraco())
            {
                return false;
            }

            var userKey = identity.GetUserKey();
            if (userKey is null)
            {
                return false;
            }

            var user = await _userService.GetAsync(userKey.Value);
            return user?.AllowedSections.Contains(Constants.Applications.Content) == true;
        }

        /// <summary>
        /// Helper method to get the link styles
        /// </summary>
        /// <param name="linkColour">The CSS colour of the link text</param>
        /// <param name="linkBackgroundColour">The CSS colour of the link background</param>
        /// <param name="linkPadding">The padding around the link</param>
        /// <param name="fontSize">The font size of the link text</param>
        /// <param name="borderRadius">The border radius of the link</param>
        /// <returns></returns>
        private static string GetLinkStyles(
            string linkColour = "#ffffff", 
            string linkBackgroundColour = "#1b264f",
            int linkPadding = 10, 
            int fontSize = 16, 
            int borderRadius = 6)
        {
            StringBuilder linkStyles = new StringBuilder();
            linkStyles.Append($"color:{linkColour};");
            linkStyles.Append($"background-color:{linkBackgroundColour};");
            linkStyles.Append($"padding:{linkPadding}px;");
            linkStyles.Append($"font-size:{fontSize}px;");
            linkStyles.Append($"border-radius:{borderRadius}px;");
            return linkStyles.ToString();
        }

        /// <summary>
        /// Helper method to get the outer element styles
        /// </summary>
        /// <param name="outerPosition">The CSS position of the outer element</param>
        /// <param name="margin">The margin around the outer element</param>
        /// <param name="zindex">The z-index of the outer element</param>
        /// <param name="linkPadding">The padding around the link</param>
        /// <returns></returns>
        private static string GetOuterElementStyles(
            string outerPosition = "fixed", 
            int margin = 10, 
            int zindex = 10000, 
            int linkPadding = 10)
        {
            linkPadding /= 2;

            var outerStyles = new StringBuilder();

            outerStyles.Append("display:block;");
            if (outerPosition == "fixed")
            {
                outerStyles.Append($"bottom:{margin + linkPadding}px;");
                outerStyles.Append($"left:{margin}px;");
            }

            outerStyles.Append($"z-index:{zindex};");
            outerStyles.Append($"position:{outerPosition};");

            return outerStyles.ToString();
        }
    }
}
