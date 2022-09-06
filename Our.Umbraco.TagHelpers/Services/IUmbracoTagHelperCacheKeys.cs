using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System.Collections.Generic;

namespace Our.Umbraco.TagHelpers.Services
{
	public interface IUmbracoTagHelperCacheKeys
	{
		Dictionary<string, CacheTagKey> CacheKeys { get; }
	}
}
