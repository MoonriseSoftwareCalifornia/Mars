using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.IdentityManagement.Website.Models
{
    public class UserRoleAssignmentsViewModel
    {
        /// <summary>
        /// User Id
        /// </summary>
        [Display(Name = "User Id")]
        public string Id { get; set; }
        /// <summary>
        /// User Email Address
        /// </summary>
        [Display(Name = "User Email Address")]
        public string Email { get; set; }

        [Display(Name = "Role Assignments")]
        public string[] RoleIds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<IdentityRole> IdentityRoles { get; set; }
    }
}
