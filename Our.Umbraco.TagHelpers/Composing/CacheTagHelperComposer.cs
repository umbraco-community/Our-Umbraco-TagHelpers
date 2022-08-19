using Our.Umbraco.TagHelpers.Notifications;
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
            builder.AddNotificationHandler<ContentCacheRefresherNotification, HandleContentCacheRefresherNotification>();
            builder.AddNotificationHandler<MediaCacheRefresherNotification, HandleMediaCacheRefresherNotification>();
            builder.AddNotificationHandler<DictionaryCacheRefresherNotification, HandleDictionaryCacheRefresherNotification>();
        }
    }
}
