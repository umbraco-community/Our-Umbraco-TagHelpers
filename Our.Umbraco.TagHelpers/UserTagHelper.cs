using Microsoft.AspNetCore.Razor.TagHelpers;
using Our.Umbraco.TagHelpers.Extensions;
using Our.Umbraco.TagHelpers.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Umbraco.Extensions;

namespace Our.Umbraco.TagHelpers
{
    /// <summary>
    /// An attribute TagHelper to help show or hide DOM elements for users
    /// 
    /// </summary>
    [HtmlTargetElement("*", Attributes = "our-user-include")]
    [HtmlTargetElement("*", Attributes = "our-user-exclude")]
    public class UserTagHelper : TagHelper
    {
        private readonly IBackofficeUserAccessor _backofficeUserAccessor;

        /// <summary>
        /// A comma separated list of User Groups to exclude
        /// ? = All anonymous users
        /// * = All authenticated users
        /// </summary>
        [HtmlAttributeName("our-user-exclude")]
        public string ExcludeGroups { get; set; }

        /// <summary>
        /// A comma separated list of User Groups to include
        /// ? = All anonymous users
        /// * = All authenticated users
        /// </summary>
        [HtmlAttributeName("our-user-include")]
        public string IncludeGroups { get; set; }

        public UserTagHelper(IBackofficeUserAccessor backofficeUserAccessor)
        {
            _backofficeUserAccessor = backofficeUserAccessor;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var currentUser = _backofficeUserAccessor.BackofficeUser;
            var currentUserGroups = new List<string>();
            if (currentUser != null)
            {
                var groups = currentUser.GetRoles();
                currentUserGroups.AddRange(groups);
            }

            // Process excluded roles
            if (!string.IsNullOrWhiteSpace(this.ExcludeGroups) && IsUserInRole(currentUser, ExcludeGroups, currentUserGroups) == true)
            {
                output.SuppressOutput();
                return;
            }

            // Process included roles
            else if (!string.IsNullOrWhiteSpace(this.IncludeGroups) && IsUserInRole(currentUser, IncludeGroups, currentUserGroups) == false)
            {
                output.SuppressOutput();
                return;
            }
        }

        private bool IsUserInRole(ClaimsIdentity currentUser, string roleString, List<string> currentMemberRoles)
        {
            //roles is a CSV of member groups they need to have access to
            var roles = roleString.Split(',').Select(x => x.Trim());
            foreach (var role in roles)
            {
                // Role ? == all anonymous users (User not logged in)
                if (role == "?" && !currentUser.IsLoggedIntoUmbraco())
                {
                    return true;
                }

                // Role * == all authenticated users
                if (role == "*" && currentUser.IsLoggedIntoUmbraco())
                {
                    return true;
                }

                if (currentMemberRoles.InvariantContains(role))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
