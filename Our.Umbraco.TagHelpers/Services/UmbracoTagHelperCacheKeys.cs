using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System.Collections.Generic;

namespace Our.Umbraco.TagHelpers.Services
{
	public class UmbracoTagHelperCacheKeys : IUmbracoTagHelperCacheKeys
	{
		public Dictionary<string,CacheTagKey> CacheKeys { get; } = new Dictionary<string,CacheTagKey>();
	}
}
