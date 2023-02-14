using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System.Collections.Concurrent;

namespace Our.Umbraco.TagHelpers.Services
{
	public interface IUmbracoTagHelperCacheKeys
	{
        ConcurrentDictionary<string, CacheTagKey> CacheKeys { get; }
	}
}
