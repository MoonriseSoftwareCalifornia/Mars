using SendGrid;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.IdentityManagement.Website.Models
{
    /// <summary>
    /// User created view model
    /// </summary>
    public class UserCreatedViewModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sendGridResponse"></param>
        public UserCreatedViewModel(UserCreateViewModel model, Response sendGridResponse)
        {
            EmailAddress = model.EmailAddress;
            EmailConfirmed = model.EmailConfirmed;
            PhoneNumber = model.PhoneNumber;
            PhoneNumberConfirmed = model.PhoneNumberConfirmed;
            GenerateRandomPassword = model.GenerateRandomPassword;
            RevealPassword = model.Password;
            SendGridResponse = sendGridResponse;
        }

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
        [Phone()]
        [Display(Name = "Telephone #")]
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

        /// <summary>
        /// Reveal password value
        /// </summary>
        [Display(Name = "Password (recommended to use random instead)")]
        public string RevealPassword { get; set; }

        /// <summary>
        /// SendGrid response
        /// </summary>
        [Display(Name = "SendGrid Response")]
        public Response SendGridResponse { get; set; }
    }
}
