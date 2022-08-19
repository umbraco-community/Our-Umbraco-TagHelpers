using Microsoft.Extensions.DependencyInjection;
using Our.Umbraco.TagHelpers.Notifications;
using Our.Umbraco.TagHelpers.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.TagHelpers.Composing
{
    public class CacheTagHelperComposer : IComposer
    {
        // handle refreshing of content/media/dictionary cache notification to clear the cache key used for the CacheTagHelper
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<IUmbracoTagHelperCacheKeys, UmbracoTagHelperCacheKeys>();

            builder.AddNotificationHandler<ContentCacheRefresherNotification, CacheTagRefresherNotifications>();
            builder.AddNotificationHandler<MediaCacheRefresherNotification, CacheTagRefresherNotifications>();
            builder.AddNotificationHandler<DictionaryCacheRefresherNotification, CacheTagRefresherNotifications>();
        }
    }
}
