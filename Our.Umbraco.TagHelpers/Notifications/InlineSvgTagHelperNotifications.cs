using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.TagHelpers.Notifications
{
    /// <summary>
    /// Clears the SVGs that <c>our-svg</c> cached from the media library when media changes.
    /// </summary>
    /// <remarks>
    /// Umbraco raises <see cref="MediaCacheRefresherNotification"/> on every server of a load-balanced site
    /// (unlike <see cref="MediaSavedNotification"/>, which is only raised where the media was saved), so every
    /// server clears its copy. All cached media SVGs are cleared rather than reading the notification's payload,
    /// whose type can change between Umbraco versions; media changes are rare and the files are re-read on demand.
    /// SVGs from wwwroot (src) aren't affected by media changes and stay cached until they expire.
    /// </remarks>
    public class InlineSvgTagHelperNotifications : INotificationHandler<MediaCacheRefresherNotification>
    {
        // Must match the cache key prefix used for media items in InlineSvgTagHelper
        internal const string MediaCacheKeyPrefix = "MediaItem-SvgContents";

        private readonly ILogger<InlineSvgTagHelperNotifications> _logger;
        private readonly AppCaches _appCaches;

        public InlineSvgTagHelperNotifications(ILogger<InlineSvgTagHelperNotifications> logger, AppCaches appCaches)
        {
            _logger = logger;
            _appCaches = appCaches;
        }

        public void Handle(MediaCacheRefresherNotification notification)
        {
            // ClearByKey removes every item whose key starts with the prefix
            _appCaches.RuntimeCache.ClearByKey(MediaCacheKeyPrefix);
            _logger.LogDebug("Media changed: cleared the inline SVGs cached from the media library");
        }
    }
}
