#nullable enable
using System;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Notifications;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace Our.Umbraco.TagHelpers.Tests
{
    // Cached media SVGs must be cleared on every server of a load-balanced site when media changes, so the
    // handler listens to MediaCacheRefresherNotification, which Umbraco raises on every server
    public class InlineSvgTagHelperNotificationsTests
    {
        [TestCase(MessageType.RefreshByPayload)]
        [TestCase(MessageType.RefreshAll)]
        [TestCase(MessageType.RefreshById)]
        [TestCase(MessageType.RemoveById)]
        public void MediaCacheRefresh_ClearsCachedMediaSvgs_AndKeepsFileSvgs(MessageType messageType)
        {
            var appCaches = new AppCaches(new ObjectCacheAppCache(), NoAppCache.Instance, new IsolatedCaches(_ => NoAppCache.Instance));
            var cache = appCaches.RuntimeCache;
            cache.Insert($"MediaItem-SvgContents ({Guid.NewGuid()})", () => "<svg>a</svg>");
            cache.Insert($"MediaItem-SvgContents ({Guid.NewGuid()})", () => "<svg>b</svg>");
            cache.Insert("File-SvgContents (/logo.svg)", () => "<svg>file</svg>");
            cache.Insert("Something-Else", () => "other");

            var handler = new InlineSvgTagHelperNotifications(NullLogger<InlineSvgTagHelperNotifications>.Instance, appCaches);
            handler.Handle(new MediaCacheRefresherNotification(Array.Empty<object>(), messageType));

            ClassicAssert.IsEmpty(cache.SearchByKey("MediaItem-SvgContents"));
            ClassicAssert.AreEqual(1, cache.SearchByKey("File-SvgContents").Count());
            ClassicAssert.AreEqual(1, cache.SearchByKey("Something-Else").Count());
        }
    }
}
