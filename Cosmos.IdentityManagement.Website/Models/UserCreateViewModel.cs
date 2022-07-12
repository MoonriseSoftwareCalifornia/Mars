using System.ComponentModel.DataAnnotations;

namespace Cosmos.IdentityManagement.Website.Models
{
    public class UserCreateViewModel
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        /// <summary>
        /// User's email address is confirmed.
        /// </summary>
        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// User's phone number (can be SMS)
        /// </summary>
        [Display(Name = "Telephone #")]
        [Phone()]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// User's phone number (can be SMS)
        /// </summary>
        [Display(Name = "Phone Confirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Optionally generates a random password
        /// </summary>
        [Display(Name = "Generate random password")]
        public bool GenerateRandomPassword { get; set; } = true;

        [Display(Name = "Password (recommended to use random instead)")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
