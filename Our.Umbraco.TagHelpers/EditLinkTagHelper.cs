using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Enums;
using Our.Umbraco.TagHelpers.Services;
using System.Linq;
using System.Text;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// If the user viewing the front end is logged in as an umbraco user
    /// then an edit link will display on the front end of the site. This will
    /// take you to the umbraco backoffice to edit the current page.
    /// </summary>
    [HtmlTargetElement("our-editlink")]
    public class EditLinkTagHelper : TagHelper
    {
        private readonly IBackofficeUserAccessor _backofficeUserAccessor;

        public EditLinkTagHelper(IBackofficeUserAccessor backofficeUserAccessor)
        {
            _backofficeUserAccessor = backofficeUserAccessor;
        }

        /// <summary>
        /// The id of the current content item. Defaults to 0
        /// </summary>
        [HtmlAttributeName("content-id")]
        public int ContentId { get; set; } = 0;

        /// <summary>
        /// An enum to say which corner of the screen you would like 
        /// the edit link to show. Defaults to bottom left.
        /// </summary>
        [HtmlAttributeName("position")]
        public EditLinkPosition Position { get; set; } = EditLinkPosition.BottomLeft;

        /// <summary>
        /// A bool to say whether or not you would like to apply the inline link styles. Defaults to true.
        /// </summary>
        [HtmlAttributeName("apply-inline-link-styles")]
        public bool ApplyInlineLinkStyles { get; set; } = true;

        /// <summary>
        /// The 'Edit' text in the link. Defaults to "Edit"
        /// </summary>
        [HtmlAttributeName("edit-message")]
        public string EditMessage { get; set; } = "Edit";

        /// <summary>
        /// The CSS colour of the link text. Defaults to #fff
        /// </summary>
        [HtmlAttributeName("link-colour")]
        public string LinkColour { get; set; } = "#fff";

        /// <summary>
        /// The CSS colour of the link background. Defaults to "#1b264f"
        /// </summary>
        [HtmlAttributeName("link-background-colour")]
        public string LinkBackgroundColour { get; set; } = "#1b264f";

        /// <summary>
        /// The font size of the link text in pixels. Defaults to 16
        /// </summary>
        [HtmlAttributeName("font-size")]
        public int FontSize { get; set; } = 16;

        /// <summary>
        /// The padding around the link in pixels. Defaults to 10
        /// </summary>
        [HtmlAttributeName("link-padding")]
        public int LinkPadding { get; set; } = 10;

        /// <summary>
        /// The border radius of the link in pixels. Defaults to 6
        /// </summary>
        [HtmlAttributeName("border-radius")]
        public int BorderRadius { get; set; } = 6;

        /// <summary>
        /// The class you would like to add to the link. Defaults to "edit-link-inner"
        /// </summary>
        [HtmlAttributeName("link-class-name")]
        public string LinkClassName { get; set; } = "edit-link-inner";

        /// <summary>
        /// Whether or not you would like to apply the inline styles for the outer element. Defaults to true
        /// </summary>
        [HtmlAttributeName("apply-inline-outer-element-styles")]
        public bool ApplyInlineOuterElementStyles { get; set; } = true;

        /// <summary>
        /// The margin around the link. Defaults to 10
        /// </summary>
        [HtmlAttributeName("margin")]
        public int Margin { get; set; } = 10;

        /// <summary>
        /// The zindex of this link block. Defaults to 10000
        /// </summary>
        [HtmlAttributeName("zindex")]
        public int Zindex { get; set; } = 10000;

        /// <summary>
        /// Override the umbraco edit content url if yours is different
        /// </summary>
        [HtmlAttributeName("umbraco-edit-content-url")]
        public string UmbracoEditContentUrl { get; set; } = "/umbraco#/content/content/edit/";

        /// <summary>
        /// The class name for the outer element. Defaults to "edit-link-outer"
        /// </summary>
        [HtmlAttributeName("outer-class-name")]
        public string OuterClassName { get; set; } = "edit-link-outer";

        /// <summary>
        /// The CSS position for the outer element. Defaults to "fixed"
        /// </summary>
        [HtmlAttributeName("outer-position")]
        public string OuterPosition { get; set; } = "fixed";

        /// <summary>
        /// The CSS position for the link. Defaults to "absolute"
        /// </summary>
        [HtmlAttributeName("link-position")]
        public string LinkPosition { get; set; } = "absolute";


        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "";

            if(_backofficeUserAccessor.BackofficeUser != null 
                && _backofficeUserAccessor.BackofficeUser.AuthenticationType 
                    == global::Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType
                && _backofficeUserAccessor.BackofficeUser.Claims != null
                && _backofficeUserAccessor.BackofficeUser.Claims.Any(x => 
                    x.Type == global::Umbraco.Cms.Core.Constants.Security.AllowedApplicationsClaimType
                    && x.Value == global::Umbraco.Cms.Core.Constants.Conventions.PermissionCategories.ContentCategory))
            {
                var editLink = $"/umbraco#/content/content/edit/{ContentId}";



                StringBuilder editLinkCode = new StringBuilder();

                //Render the starting outer div
                editLinkCode.Append($"<div");
                editLinkCode.Append($" class=\"{OuterClassName}\"");

                //Render the inline styles for the outer div
                if (ApplyInlineOuterElementStyles)
                {
                    string outerStyles = GetOuterElementStyles(OuterPosition, Position, Margin, Zindex, LinkPadding);
                    editLinkCode.Append($" style=\"{outerStyles}\"");
                }
                editLinkCode.Append($">");

                //Render the link
                editLinkCode.Append($"<a href=\"{UmbracoEditContentUrl}{ContentId}\"");
                editLinkCode.Append($" target=\"_blank\"");
                editLinkCode.Append($" class=\"{LinkClassName}\"");

                //Render the inline styles for the link
                if (ApplyInlineLinkStyles)
                {
                    string linkStyles = GetLinkStyles(LinkColour, LinkBackgroundColour, LinkPadding, FontSize, BorderRadius);
                    editLinkCode.Append($"style=\"{linkStyles}\"");
                }

                //Render the link text and closing tag
                editLinkCode.Append($">{EditMessage}</a>");

                //Render the closing outer div
                editLinkCode.Append($"</div>");
                output.Content.SetHtmlContent(editLinkCode.ToString());
                return;
            }
            else
            {
                output.SuppressOutput();
                return;
            }
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
        private static string GetLinkStyles(string linkColour, string linkBackgroundColour,
            int linkPadding, int fontSize, int borderRadius)
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
        /// <param name="position">The CSS position of the link element</param>
        /// <param name="margin">The margin around the outer element</param>
        /// <param name="zindex">The zindex of the outer element</param>
        /// <param name="linkPadding">The padding around the link</param>
        /// <returns></returns>
        private static string GetOuterElementStyles(string outerPosition, EditLinkPosition position,
            int margin, int zindex, int linkPadding)
        {
            linkPadding = linkPadding / 2;

            StringBuilder outerStyles = new StringBuilder();

            outerStyles.Append("display:block;");
            if (outerPosition == "fixed")
            {
                switch (position)
                {
                    case EditLinkPosition.TopLeft:
                        outerStyles.Append($"top:{margin + linkPadding}px;");
                        outerStyles.Append($"left:{margin}px;");
                        break;
                    case EditLinkPosition.TopRight:
                        outerStyles.Append($"top:{margin + linkPadding}px;");
                        outerStyles.Append($"right:{margin}px;");
                        break;
                    case EditLinkPosition.BottomRight:
                        outerStyles.Append($"bottom:{margin + linkPadding}px;");
                        outerStyles.Append($"right:{margin}px;");
                        break;
                    case EditLinkPosition.BottomLeft:
                        outerStyles.Append($"bottom:{margin + linkPadding}px;");
                        outerStyles.Append($"left:{margin}px;");
                        break;
                }
            }

            outerStyles.Append($"z-index:{zindex};");
            outerStyles.Append($"position:{outerPosition};");

            return outerStyles.ToString();
        }
    }
}