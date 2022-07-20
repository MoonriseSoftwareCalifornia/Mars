using AspNetCore.Identity.Services.SendGrid;
using Cosmos.IdentityManagement.Website.Models;
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

        public async Task<IActionResult> GetUsers(string text)
        {
            var query = _userManager.Users.OrderBy(o => o.Email)
                .Select(
                  s => new
                  {
                      s.Id,
                      s.Email
                  }
                ).AsQueryable();

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(s => s.Email.ToLower().StartsWith(text.ToLower()));
            }

            var users = await query.ToListAsync();

            return Json(users);
        }

        /// <summary>
        /// Page designed to add/remove users from a single role.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> UsersInRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var model = new UsersInRoleViewModel()
            {
                RoleId = role.Id,
                RoleName = role.Name
            };
            return View(model);
        }

        /// <summary>
        /// Saves changes to the user assignments in a role
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsersInRole(UsersInRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                foreach(var id in model.UserIds)
                {
                    var user = await _userManager.FindByIdAsync(id);
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                }

                model.UserIds = null;

                return View(model);
            }

            // Not valid, return the selected users.
            model.Users = await _userManager.Users.Where(w => model.UserIds.Contains(w.Id))
                .Select(
                s => new SelectedUserViewModel()
                {
                    Id = s.Id,
                    Email =s.Email
                }
                ).ToListAsync();

            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <param name="Id">Role Id</param>
        /// <returns></returns>
        public async Task<IActionResult> BulkDelete_Users([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<UserIndexViewModel> users, string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return NotFound();
            }

            if (users != null && ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(Id);

                foreach (var user in users)
                {
                    // Make sure there is at least one administrator remaining
                    var administrators = await _userManager.GetUsersInRoleAsync("User Administrators");

                    if (administrators.Count() > 1)
                    {
                        var userId = user.UserId;

                        var identityUser = await _userManager.FindByIdAsync(userId);

                        await _userManager.RemoveFromRoleAsync(identityUser, role.Name);
                    }
                }
            }

            return Json(await users.ToDataSourceResultAsync(request, ModelState));
        }

    }
}
