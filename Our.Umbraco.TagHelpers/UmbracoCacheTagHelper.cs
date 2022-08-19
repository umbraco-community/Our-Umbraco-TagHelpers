using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.CacheKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

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
		private readonly IAppPolicyCache _runtimeCache;
		// default to true, a very 'Umbraco' convention.
		private bool _updateCacheKeyOnPublish = true;

		/// <summary>
		/// Whether to update the cache key when any content, media, dictionary item is published in Umbraco.
		/// </summary>
		public bool UpdateCacheKeyOnPublish
		{
			get { return _updateCacheKeyOnPublish; }
			set { _updateCacheKeyOnPublish = value; }
		}

		public UmbracoCacheTagHelper(CacheTagHelperMemoryCacheFactory factory, HtmlEncoder htmlEncoder, AppCaches appCaches, IUmbracoContextFactory umbracoContextFactory) : base(factory, htmlEncoder)
		{
			_umbracoContextFactory = umbracoContextFactory;
			_runtimeCache = appCaches.RuntimeCache;
		}
		public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
		{
			using (UmbracoContextReference umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext())
			{
				var umbracoContext = umbracoContextReference.UmbracoContext;
				// we don't want to enable the cache tag helper if Umbraco is in Preview, or in Debug mode
				if (umbracoContext.InPreviewMode || umbracoContext.IsDebug)
				{
					this.Enabled = false;
					await output.GetChildContentAsync();
				}
				else
				{
					// with the CacheTagHelper we are wrapping here it's really difficult to clear the cache of the Tag Helper output 'on demand'
					// eg when a page is published, with Umbraco's CachedPartial that's what happens, so if you change the name of a page, and the site navigation
					// is cached with a cached partial, the cache is automatically cleared.
					// it seems for CacheTagHelper the .net core advice when you want to break the cache is to update the varyby key, the previous key in the cache will be forgotten
					// and fall out of the cache naturally... (but I did later on find this article: https://www.umbrajobs.com/blog/posts/2021/june/umbraco-9-net-core-caching-part-1-cashing-shared-partial-views/
					// where it talks about being able to clear the actual cache tag using reflection.. - it won't work on loadbalanced servers - using wrong notifications!
					// ... so maybe that's the way people will ultimately prefer to go...
					// but for this tag helper we just track the last time a peace of content, dictionary item or media was published, and use that datetime in the varyby cachekey
					// so everytime something is published in Umbraco, the cache tag helper will have a different cache key and this will produce a new cached result
					// this might be a bad thing?
					// so we have a setting to turn this off, so the tag helper is still usable as the existing .net core cache tag helper, without caching in preview or umbraco debug
					// which is still handyish
					if (_updateCacheKeyOnPublish)
					{
						// ironically read the last cache refresh date from runtime cache, and set it to now if it's not there...
						var umbLastCacheRefreshCacheKey = _runtimeCache.GetCacheItem(CacheKeyConstants.LastCacheRefreshDateKey, () => GetFallbackCacheRefreshDate()).ToString();
						// append to VaryBy key incase VaryBy key is set to some other parameter too
						this.VaryBy = umbLastCacheRefreshCacheKey + "|" + this.VaryBy;
						// if an expiry date isn't set when using the CacheTagHelper, let's add one to be 24hrs, so when mulitple publishes occur, the versions of this taghelper don't hang around forever
						if (this.ExpiresAfter == null)
                        {
							this.ExpiresAfter = new TimeSpan(24, 0, 0);
                        }
					}

					await base.ProcessAsync(context, output);
				}
			}

		}
		private string GetFallbackCacheRefreshDate()
		{
			//this fires if the 'appcache' doesn't have a LastCacheRefreshDate set by a publish
			// eg after an app pool recycle
			// it doesn't really matter that this isn't the last datetime that something was actually published
			// because time tends to always move forwards
			// the next publish will set a new future LastCacheRefreshDate...
			return DateTime.UtcNow.ToString("s");

		}

	}
}
