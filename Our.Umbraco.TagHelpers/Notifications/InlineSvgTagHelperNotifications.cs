using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers.Notifications
{
    public class InlineSvgTagHelperNotifications : INotificationHandler<MediaSavedNotification>
    {
        private readonly ILogger<InlineSvgTagHelperNotifications> _logger;
        private readonly AppCaches _appCaches;

        public InlineSvgTagHelperNotifications(ILogger<InlineSvgTagHelperNotifications> logger, AppCaches appCaches)
        {
            _logger = logger;
            _appCaches = appCaches;
        }

        public void Handle(MediaSavedNotification notification)
        {
            foreach (var mediaItem in notification.SavedEntities)
            {
                if (mediaItem.ContentType.Alias.Equals("umbracoMediaVectorGraphics", StringComparison.InvariantCultureIgnoreCase))
                {
                    var cacheKey = string.Concat("MediaItem-SvgContents (", mediaItem.Key.ToString(), ")");
                    if (_appCaches.RuntimeCache.SearchByKey(cacheKey).Any())
                    {
                        _appCaches.RuntimeCache.ClearByKey(cacheKey);
                        _logger.LogDebug("Removed {MediaItemName} from RuntimeCache", mediaItem.Name);
                    }
                }
            }
        }
    }
}
