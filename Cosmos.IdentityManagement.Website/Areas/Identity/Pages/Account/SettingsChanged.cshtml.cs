using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cosmos.IdentityManagement.Website.Areas.Identity.Pages.Account
{
    public class SettingsChangedModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public SettingsChangedModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }
        public async Task OnGet()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

        }
    }
}
