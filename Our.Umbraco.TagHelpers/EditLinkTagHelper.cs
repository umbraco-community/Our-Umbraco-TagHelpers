using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Extensions;
using Our.Umbraco.TagHelpers.Services;
using System.Text;

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
        /// Override the umbraco edit content url if yours is different
        /// </summary>
        [HtmlAttributeName("edit-url")]
        public string EditUrl { get; set; } = "/umbraco#/content/content/edit/";

        /// <summary>
        /// Override the text of the link
        /// </summary>
        [HtmlAttributeName("text")]
        public string Text { get; set; } = "Edit";

        /// <summary>
        /// A boolean to say whether or not you would like to use the default styling.
        /// </summary>
        [HtmlAttributeName("use-default-styles")]
        public bool UseDefaultStyles { get; set; } = false;

        /// <summary>
        /// Set the id attribute if you want
        /// </summary>
        [HtmlAttributeName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// The class attribute for the link
        /// </summary>
        [HtmlAttributeName("class")]
        public string Class { get; set; } = "";

        /// <summary>
        /// Set the target attribute if you want. Defaults to _blank
        /// </summary>
        [HtmlAttributeName("target")]
        public string Target { get; set; } = "_blank";

        /// <summary>
        /// Add some inline styles to the link if you want
        /// </summary>
        [HtmlAttributeName("style")]
        public string Style { get; set; } = "";

        /// <summary>
        /// Set the title attribute if you want
        /// </summary>
        [HtmlAttributeName("title")]
        public string Title { get; set; } = "";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            //don't output the tag name as it's not valid HTML
            output.TagName = "";

            //check if the user is logged in to the backoffice
            //and they have access to the content section
            if(_backofficeUserAccessor.BackofficeUser.IsAllowedToSeeEditLink())
            {
                var editLinkUrl = $"{EditUrl}{ContentId}";

                StringBuilder editLinkCode = new StringBuilder();
                SetAttributeValue("style", Style, editLinkCode);

                if (UseDefaultStyles)
                {
                    //Render the outer div with some inline styles
                    editLinkCode.Append($"<div");
                    SetAttributeValue("style", GetOuterElementStyles(), editLinkCode);
                    editLinkCode.Append($">");
                }

                //Add the opening tag of the link
                editLinkCode.Append($"<a");

                SetAttributeValue("href", editLinkUrl, editLinkCode);
                SetAttributeValue("id", Id, editLinkCode);
                SetAttributeValue("class", Class, editLinkCode);
                SetAttributeValue("target", Target, editLinkCode);
                SetAttributeValue("title", Title, editLinkCode);

                if (UseDefaultStyles)
                {
                    SetAttributeValue("style", GetLinkStyles(), editLinkCode);
                }

                //Add the link text and close the link tag
                editLinkCode.Append($">{Text}</a>");

                if (UseDefaultStyles)
                {
                    //Add the closing outer div
                    editLinkCode.Append($"</div>");
                }

                //Set the content of the tag helper
                output.Content.SetHtmlContent(editLinkCode.ToString());
                return;
            }
            else
            {
                output.SuppressOutput();
                return;
            }
        }

        private static void SetAttributeValue(string attributeName, string attributeValue, StringBuilder editLinkCode)
        {
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                editLinkCode.Append($" {attributeName}=\"{attributeValue}\"");
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