﻿using Microsoft.Extensions.DependencyInjection;
using Our.Umbraco.TagHelpers.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.Umbraco.TagHelpers.Composing
{
    public class BackofficeUserComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IBackofficeUserAccessor, BackofficeUserAccessor>();
        }
    }
}
