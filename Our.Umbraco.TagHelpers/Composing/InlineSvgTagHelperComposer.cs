using Our.Umbraco.TagHelpers.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.TagHelpers.Composing
{
    public class InlineSvgTagHelperComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationHandler<MediaSavedNotification, InlineSvgTagHelperNotifications>();
        }
    }
}
