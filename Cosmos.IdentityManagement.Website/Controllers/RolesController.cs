using AspNetCore.Identity.Services.SendGrid;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cosmos.IdentityManagement.Website.Controllers
{
    [Authorize(Roles = "User Administrators")]
    public class RolesController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SendGridEmailSender _emailSender;

        public RolesController(
               ILogger<HomeController> logger,
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
        public IActionResult Index([Bind("ids")] string ids)
        {
            if (string.IsNullOrEmpty(ids))
                return View();

            var model = ids.Split(',');
            return View(model);
        }

        public async Task<IActionResult> BulkCreate_Roles([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "models")] IEnumerable<IdentityRole> models)
        {
            var results = new List<IdentityRole>();

            if (models != null && ModelState.IsValid)
            {
                foreach (var role in models)
                {
                    role.Id = Guid.NewGuid().ToString();
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        var identityRole = await _roleManager.FindByIdAsync(role.Id);

                        await _roleManager.SetRoleNameAsync(identityRole, role.Name);
                        await _roleManager.UpdateNormalizedRoleNameAsync(identityRole);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", $"Error code: {error.Code}. Message: {error.Description}");
                        }
                    }
                }
            }

            return Json(results.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Reads a list of users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Roles([DataSourceRequest] DataSourceRequest request)
        {
            var model = await _roleManager.Roles.OrderBy(o => o.Name).ToListAsync();
            return Json(await model.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Updates role names
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> BulkUpdate_Roles([DataSourceRequest] DataSourceRequest request,
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
        public async Task<ActionResult> BulkDelete_Roles([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<IdentityRole> roles)
        {

            if (roles != null && ModelState.IsValid)
            {
                if (roles.Any(a => a.Name.Equals("User Administrators", StringComparison.InvariantCultureIgnoreCase)))
                {
                    ModelState.AddModelError("", "Cannot remove the User Administrators role.");
                }

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
