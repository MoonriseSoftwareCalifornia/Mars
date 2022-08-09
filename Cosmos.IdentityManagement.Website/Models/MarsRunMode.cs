namespace Cosmos.IdentityManagement.Website.Models
{
    public class MarsRunMode
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="setup">Setup database on run</param>
        /// <param name="sendGridSandboxMode">SendGrid SandBox Mode</param>
        /// <param name="gTag">Google Analytics GTag</param>
        public MarsRunMode(string setup, string sendGridSandboxMode, string gTag = "")
        {
            bool.TryParse(setup, out var boolSetup);
            bool.TryParse(sendGridSandboxMode, out var boolSendGridSandboxMode);

            Setup = boolSetup;
            SendGridSandboxMode = boolSendGridSandboxMode;
            GTag = gTag;
        }

        /// <summary>
        /// Google Analytics Tag
        /// </summary>
        public string GTag { get; set; } = string.Empty;

        /// <summary>
        /// Setup the database on run
        /// </summary>
        public bool Setup { get; }

        /// <summary>
        /// Enable SendGrid sandbox mode
        /// </summary>
        public bool SendGridSandboxMode { get; }
    }
}
