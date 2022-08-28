using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using System.Collections.Generic;

namespace Our.Umbraco.TagHelpers.Services
{
	/// <summary>
	/// used to retain a list of hashed cache tag helper keys that have been created using the our cache tag helper
	/// and which need to be cleared when a publish notification takes place.
	/// </summary>
	public class UmbracoTagHelperCacheKeys : IUmbracoTagHelperCacheKeys
	{
		public Dictionary<string,CacheTagKey> CacheKeys { get; } = new Dictionary<string,CacheTagKey>();
	}
}
