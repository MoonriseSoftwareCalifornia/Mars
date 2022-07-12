using AspNetCore.Identity.Services.SendGrid;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cosmos.IdentityManagement.Website.Controllers
{
    [Authorize("User Administrators")]
    public class RolesController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SendGridEmailSender _emailSender;

        public RolesController(
               ILogger<UsersController> logger,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender
            )
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = (SendGridEmailSender)emailSender;
        }
        public IActionResult Index()
        {
            return View();
        }


        /// <summary>
        /// Reads a list of users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Roles([DataSourceRequest] DataSourceRequest request)
        {
            return Json(await _roleManager.Roles.OrderBy(o => o.Name).ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Updates role names
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Update_Roles([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<IdentityRole> roles)
        {
            if (roles != null && ModelState.IsValid)
            {
                foreach (var role in roles)
                {
                    var identityRole = await _roleManager.FindByIdAsync(role.Id);

                    await _roleManager.SetRoleNameAsync(identityRole, role.Name);
                    await _roleManager.UpdateNormalizedRoleNameAsync(identityRole);
                }
            }

            return Json(await roles.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Updates role names
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Delete_Roles([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<IdentityRole> roles)
        {

            if (roles.Any(a => a.Name.Equals("User Administrators", StringComparison.InvariantCultureIgnoreCase)))
            {
                ModelState.AddModelError("", "Cannot remove the User Administrators role.");
            }

            if (roles != null && ModelState.IsValid)
            {
                foreach (var role in roles)
                {
                    var identityRole = await _roleManager.FindByIdAsync(role.Id);

                    await _roleManager.DeleteAsync(identityRole);
                }
            }

            return Json(await roles.ToDataSourceResultAsync(request, ModelState));
        }
    }
}
