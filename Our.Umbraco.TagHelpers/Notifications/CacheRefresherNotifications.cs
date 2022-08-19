using Our.Umbraco.TagHelpers.CacheKeys;
using System;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.TagHelpers.Notifications
{
	// For Use with the Our Cache TagHelper - to store a last cache updated datetime to use
	// in the varyby key for the Cache TagHelper, to naively break the cache on publish.
	// Used for ContentCacheRefresher (Load balanced scernarios not just Published event)
	// Same for Dictionary Items and Media item
	public class HandleContentCacheRefresherNotification : INotificationHandler<ContentCacheRefresherNotification>
	{
		private readonly IAppPolicyCache _runtimeCache;

		public HandleContentCacheRefresherNotification(AppCaches appCaches)
		{
			_runtimeCache = appCaches.RuntimeCache;

		}

		public void Handle(ContentCacheRefresherNotification notification)
		{
			// fired when content published
			// store DateTime, as the cachekey
			var lastCacheRefreshDate = DateTime.UtcNow.ToString("s");

			// insert and override existing value in appcache
			_runtimeCache.Insert(CacheKeyConstants.LastCacheRefreshDateKey, () => lastCacheRefreshDate, null, false, null);

		}
	}

	public class HandleDictionaryCacheRefresherNotification : INotificationHandler<DictionaryCacheRefresherNotification>
	{
		private readonly IAppPolicyCache _runtimeCache;
		public HandleDictionaryCacheRefresherNotification(AppCaches appCaches)
		{
			_runtimeCache = appCaches.RuntimeCache;
		}

		public void Handle(DictionaryCacheRefresherNotification notification)
		{
			// fired when Dictionary item updated
			// store DateTime, as the cachekey
			var lastCacheRefreshDate = DateTime.UtcNow.ToString("s");

			// insert and override existing value in appcache
			_runtimeCache.Insert(CacheKeyConstants.LastCacheRefreshDateKey, () => lastCacheRefreshDate, null, false, null);

		}
	}

	public class HandleMediaCacheRefresherNotification : INotificationHandler<MediaCacheRefresherNotification>
	{
		private readonly IAppPolicyCache _runtimeCache;
		public HandleMediaCacheRefresherNotification(AppCaches appCaches)
		{
			_runtimeCache = appCaches.RuntimeCache;
		}

		public void Handle(MediaCacheRefresherNotification notification)
		{
			// fired when media updated
			// store DateTime, as the cachekey
			var lastCacheRefreshDate = DateTime.UtcNow.ToString("s");

			// insert and override existing value in appcache
			_runtimeCache.Insert(CacheKeyConstants.LastCacheRefreshDateKey, () => lastCacheRefreshDate, null, false, null);
		}
	}
}
