using Our.Umbraco.TagHelpers.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
