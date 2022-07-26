namespace Cosmos.IdentityManagement.Website.Models
{
    public class MarsRunMode
    {
        public MarsRunMode(string setup, string sendGridSandboxMode)
        {
            bool.TryParse(setup, out var boolSetup);
            bool.TryParse(sendGridSandboxMode, out var boolSendGridSandboxMode);

            Setup = boolSetup;
            SendGridSandboxMode = boolSendGridSandboxMode;
        }

        public bool Setup { get; }

        public bool SendGridSandboxMode { get; }
    }
}
