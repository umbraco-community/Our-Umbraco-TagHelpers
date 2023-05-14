using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Templates;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers
{
	/// <summary>
	/// Render an Umbraco Form in your views
	/// </summary>
	[HtmlTargetElement("our-umb-form")]
	public class UmbFormTagHelper : TagHelper
	{
		private readonly IUmbracoComponentRenderer _umbracoComponentRenderer;
		private readonly IUmbracoContextAccessor _umbracoContextAccessor;

		private const string RenderUmbracoFormMacroAlias = "renderUmbracoForm";
		private const string FormGuidMacroParameterName = "FormGuid";
		private const string FormThemeMacroParameterName = "FormTheme";
		private const string ExcludeScriptsMacroParameterName = "ExcludeScripts";

		/// <summary>
		/// The Guid of the Form to output
		/// </summary>
		[HtmlAttributeName("form-guid")]
		public string? Guid { get; set; }

		/// <summary>
		/// The name of the theme to use to render the form
		/// </summary>
		[HtmlAttributeName("theme")]
		public string? Theme { get; set; }

        /// <summary>
        /// Whether you want to render the associated scripts with a Form
        /// </summary>
        [HtmlAttributeName("exclude-scripts")]
		public bool? ExcludeScripts { get; set; }

		public UmbFormTagHelper(IUmbracoComponentRenderer umbracoComponentRenderer, IUmbracoContextAccessor umbracoContextAccessor)
		{
			_umbracoComponentRenderer = umbracoComponentRenderer;
			_umbracoContextAccessor = umbracoContextAccessor;
		}

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			output.TagName = string.Empty; // Remove the outer <our-form> tag

			IPublishedContent? contentNode = null;

			if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
			{
				contentNode = umbracoContext.PublishedRequest.PublishedContent;
			}

			var macroParams = new Dictionary<string, object?>
			{
				{ FormGuidMacroParameterName, Guid },

				{ FormThemeMacroParameterName, Theme },

				// the exclude scripts macro parameter needs to have a value of 0 or 1
				{ ExcludeScriptsMacroParameterName, Convert.ToInt32(ExcludeScripts) },
			};

			var macroResult = await _umbracoComponentRenderer.RenderMacroForContent(contentNode, RenderUmbracoFormMacroAlias, macroParams);

			output.Content.SetHtmlContent(macroResult.ToHtmlString());
		}
	}
}
