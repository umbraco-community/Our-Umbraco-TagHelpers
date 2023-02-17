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
    /// A wrapper around .net core CacheTagHelper, that is Umbraco Aware - so won't cache in Preview or Debug Mode
    /// And will automatically clear it's when anything is published (optional)
    /// </summary>
    [HtmlTargetElement("our-cache")]
    public class UmbracoCacheTagHelper : CacheTagHelper
    {
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IUmbracoTagHelperCacheKeys _cacheKeys;

        /// <summary>
        /// Whether to update the cache key when any content, media, dictionary item is published in Umbraco.
        /// </summary>
        public bool UpdateCacheOnPublish { get; set; } = true;

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
                    // Set the enabled flag to false & let base class
                    // of the cache tag helper do the disabling of the cache
                    this.Enabled = false;
                }
                else
                {
                    // Now whenever anything is published in Umbraco 'the old Umbraco Cache Helper convention' was to clear out all the view memory caches
                    // we want to do the same by default for this tag helper
                    // we can't track by convention as dot net tag helper cache key is hashed - but we can generte the hash key here, and add it to a dictionary
                    // which we can loop through when the Umbraco cache is updated to clear the tag helper cache.
                    // you can opt out of this by setting update-cache-on-publish="false" in the individual tag helper
                    if (UpdateCacheOnPublish)
                    {
                        // The base TagHelper would generate it's own CacheTagKey to create a unique hash
                        // but if we call it here 'too' it will fortunately be the same hash.
                        // so we can keep track of it & put into some dictionary or collection
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
