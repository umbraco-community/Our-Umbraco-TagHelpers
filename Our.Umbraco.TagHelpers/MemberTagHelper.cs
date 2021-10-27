using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Security;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// 
    /// </summary>
    [HtmlTargetElement("*", Attributes = "our-member-include")]
    [HtmlTargetElement("*", Attributes ="our-member-exclude")]
    public class MemberTagHelper : TagHelper
    {
        private IMemberManager _memberManager;

        /// <summary>
        /// 
        /// </summary>
        [HtmlAttributeName("our-member-exclude")]
        public string ExcludeRoles { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [HtmlAttributeName("our-member-include")]
        public string IncludeRoles { get; set; }

        public MemberTagHelper(IMemberManager memberManager)
        {
            _memberManager = memberManager;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var currentMember = await _memberManager.GetCurrentMemberAsync();
            var currentMemberRoles = new List<string>();
            if(currentMember != null)
            {
                currentMemberRoles.AddRange(await _memberManager.GetRolesAsync(currentMember));
            }

            // Process excluded roles
            if (!string.IsNullOrWhiteSpace(this.ExcludeRoles) && IsUserInRole(ExcludeRoles, currentMemberRoles) == true)
            {
                output.SuppressOutput();
                return;
            }

            // Process included roles
            else if (!string.IsNullOrWhiteSpace(this.IncludeRoles) && IsUserInRole(IncludeRoles, currentMemberRoles) == false)
            {
                output.SuppressOutput();
                return;
            }
        }

        private bool IsUserInRole(string roleString, List<string> currentMemberRoles)
        {
            // roles is a CSV of member groups they need to have access to
            var roles = roleString.Split(',').Select(x => x.Trim());
            foreach (var role in roles)
            {
                // Role ? == all anonymous users (User not logged in)
                if (role == "?" && _memberManager.IsLoggedIn() == false)
                {
                    return true;
                }

                // Role * == all authenticated users
                if (role == "*" && _memberManager.IsLoggedIn())
                {
                    return true;
                }

                if (currentMemberRoles.Contains(role))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
