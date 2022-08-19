using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Services;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace Our.Umbraco.TagHelpers
{
	/// <summary>
	/// A wrapper around .net core CacheTagHelper so you remember not to cache in preview or Umbraco debug mode
	/// Also can create a new variance of the cache when anything is published in Umbraco if that is desirable
	/// </summary>
	[HtmlTargetElement("our-cache")]
	public class UmbracoCacheTagHelper : CacheTagHelper
    {
		private readonly IUmbracoContextFactory _umbracoContextFactory;
		private readonly IUmbracoTagHelperCacheKeys _cacheKeys;

		/// <summary>
		/// Whether to update the cache key when any content, media, dictionary item is published in Umbraco.
		/// </summary>
		public bool UpdateCacheKeyOnPublish { get; set; } = true;

		public UmbracoCacheTagHelper(CacheTagHelperMemoryCacheFactory factory, 
			HtmlEncoder htmlEncoder, 
			IUmbracoContextFactory umbracoContextFactory,
            IUmbracoTagHelperCacheKeys cacheKeys) 
			: base(factory, htmlEncoder)
		{
			_umbracoContextFactory = umbracoContextFactory;
			_cacheKeys = cacheKeys;
		}

		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			using (UmbracoContextReference umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext())
			{
				var umbracoContext = umbracoContextReference.UmbracoContext;

				// we don't want to enable the cache tag helper if Umbraco is in Preview, or in Debug mode
				if (umbracoContext.InPreviewMode || umbracoContext.IsDebug)
				{
					// Set the endabled flag to false & lest base class
					// of the cache tag helper do the same stuff as before
					this.Enabled = false;
				}
				else
				{
					// Defaults to true - have to explicitly opt out with attribute set to false in Razor
					if (UpdateCacheKeyOnPublish)
					{
                        // So before we go into our base class
                        // Grab the cache key so we can keep track of it & put into some dictionary or collection
                        // and clear all items out in that collection with our notifications on publish

                        var cacheKey = new CacheTagKey(this, context);
                        var key = cacheKey.GenerateKey();
                        var hashedKey = cacheKey.GenerateHashedKey();
						_cacheKeys.CacheKeys.TryAdd(key, cacheKey);
                    }
				}

                await base.ProcessAsync(context, output);
            }
		}
	}
}
