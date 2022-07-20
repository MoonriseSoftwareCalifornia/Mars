using System.ComponentModel.DataAnnotations;

namespace Cosmos.IdentityManagement.Website.Models
{
    /// <summary>
    /// Users in a Role
    /// </summary>
    public class UsersInRoleViewModel
    {
        /// <summary>
        /// Role Id
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// Role Name
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// New user Ids
        /// </summary>
        [Required(ErrorMessage = "Please select at least one.")]
        [Display(Name = "New Users for Role")]
        public string[] UserIds { get; set; }

        public List<SelectedUserViewModel> Users { get; set; } = new List<SelectedUserViewModel>();
    }

    public class SelectedUserViewModel
    {
        public string Id { get; set; }

        public string Email { get; set; }
    }
}
