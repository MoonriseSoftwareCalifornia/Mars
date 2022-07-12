namespace Cosmos.IdentityManagement.Website.Models
{
    /// <summary>
    /// Resend email confirmation result.
    /// </summary>
    public class ResendEmailConfirmResult
    {
        /// <summary>
        /// Email sent successfully.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message (if any)
        /// </summary>
        public string Error { get; set; }
    }
}
