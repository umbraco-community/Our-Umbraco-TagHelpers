using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Our.Umbraco.TagHelpers.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.TagHelpers.Notifications
{
	/// <summary>
	/// For Use with the Our Cache TagHelper
	/// We handle the published cache updating notification for content, media and dictionary
	/// And then use our dictionary collection of tracked tag helper caches, created in the tag cache helper
	/// loop through each one and clear the tag helpers cache
	/// </summary>
	public class CacheTagRefresherNotifications : 
		INotificationHandler<ContentCacheRefresherNotification>,
        INotificationHandler<DictionaryCacheRefresherNotification>,
        INotificationHandler<MediaCacheRefresherNotification>
    {

		private IMemoryCache _memoryCache;

		private IUmbracoTagHelperCacheKeys _cacheKeys;

		public CacheTagRefresherNotifications(CacheTagHelperMemoryCacheFactory cacheFactory, IUmbracoTagHelperCacheKeys cacheKeys)
		{
            _memoryCache = cacheFactory.Cache;
			_cacheKeys = cacheKeys;
        }

		public void Handle(ContentCacheRefresherNotification notification) => ClearUmbracoTagHelperCache();

		public void Handle(DictionaryCacheRefresherNotification notification) => ClearUmbracoTagHelperCache();

		public void Handle(MediaCacheRefresherNotification notification) => ClearUmbracoTagHelperCache();

		private void ClearUmbracoTagHelperCache()
		{
			// Loop over items in dictionary
			foreach (var item in _cacheKeys.CacheKeys)
			{
				// The value stores the CacheTagKey object
				// Looking at src code from MS TagHelper that use this object itself as the key

				// Remove item from IMemoryCache
				_memoryCache.Remove(item.Value);
			}

			// Once all items cleared from IMemoryCache that we are tracking
			// Clear the dictionary out
			// It will fill back up once an <our-cache> TagHelper is called/used on a page
			_cacheKeys.CacheKeys.Clear();
		}
	}
}
