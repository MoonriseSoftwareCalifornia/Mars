using AspNetCore.Identity.Services.SendGrid;
using Cosmos.IdentityManagement.Website.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Encodings.Web;

namespace Cosmos.IdentityManagement.Website.Controllers
{
    // See: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/areas?view=aspnetcore-3.1
    /// <summary>
    /// User management controller
    /// </summary>
    [Authorize(Roles = "User Administrators")]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SendGridEmailSender _emailSender;

        public UsersController(
            ILogger<UsersController> logger,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = (SendGridEmailSender) emailSender;
        }

        /// <summary>
        ///     User manager home page
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {

            if (string.IsNullOrEmpty(model.Password) && model.GenerateRandomPassword == false)
            {
                ModelState.AddModelError("GenerateRandomPassword", "Must generate password if one not given.");
            }

            if (!ModelState.IsValid)
                return View(model);

            if (model.GenerateRandomPassword)
            {
                var password = new PasswordGenerator.Password();
                model.Password = password.Next();
            }

            var user = Activator.CreateInstance<IdentityUser>();



            await _userManager.SetUserNameAsync(user, model.EmailAddress);

            user.EmailConfirmed = model.EmailConfirmed;

            await _userManager.SetEmailAsync(user, model.EmailAddress);

            await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);

            user.PhoneNumberConfirmed = model.PhoneNumberConfirmed;

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!model.EmailConfirmed)
                {
                    var userId = user.Id;

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = "/" },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(model.EmailAddress, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                }
            }

            return View("UserCreated", model);
        }

        /// <summary>
        ///     Gets the role membership for a user by id
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns></returns>
        public async Task<IActionResult> RoleMembership([Bind("id")] string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            ViewData["saved"] = null;

            var roleList = (await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(id))).ToList();


            return View();
        }

        public async Task<IActionResult> Delete_Users([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<UserIndexViewModel> users)
        {
            if (_userManager.Users.Count() < 2)
            {
                ModelState.AddModelError("", "Cannot delete the last user account.");
            }

            if (users != null && ModelState.IsValid)
            {
                foreach (var user in users)
                {
                    var userId = user.UserId;

                    var identityUser = await _userManager.FindByIdAsync(userId);

                    var roles = await _userManager.GetRolesAsync(identityUser);

                    if (roles.Any(a => a.Equals("User Administrators", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        ModelState.AddModelError("", "Cannot remove a member of the User Administrators role.");
                    }

                    await _userManager.DeleteAsync(identityUser);
                }
            }

            return Json(await users.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Reads a list of users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Users([DataSourceRequest] DataSourceRequest request, string roleId = "")
        {
            var users = new List<UserIndexViewModel>();

            try
            {

                if (string.IsNullOrEmpty(roleId))
                {
                    users.AddRange(await _userManager.Users.Select(s => new UserIndexViewModel
                    {
                        UserId = s.Id,
                        EmailAddress = s.Email,
                        EmailConfirmed = s.EmailConfirmed,
                        PhoneNumber = s.PhoneNumber,
                        IsLockedOut = s.LockoutEnd.HasValue ? s.LockoutEnd < DateTimeOffset.UtcNow : false
                    }).OrderBy(o => o.EmailAddress).ToArrayAsync());

                }
                else
                {
                    var identityRole = await _roleManager.FindByIdAsync(roleId);

                    var usersInRole = await _userManager.GetUsersInRoleAsync(identityRole.Name);

                    users.AddRange(usersInRole.Select(s => new UserIndexViewModel
                    {
                        UserId = s.Id,
                        EmailAddress = s.Email,
                        EmailConfirmed = s.EmailConfirmed,
                        PhoneNumber = s.PhoneNumber,
                        IsLockedOut = s.LockoutEnd.HasValue ? s.LockoutEnd < DateTimeOffset.UtcNow : false
                    }).OrderBy(o => o.EmailAddress).ToArray());
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return Json(await users.ToDataSourceResultAsync(request));
        }

        /// <summary>
        /// Updates email confirmed or phone number confirmed for a set of users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Update_Users([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<UserIndexViewModel> users)
        {
            if (users != null && ModelState.IsValid)
            {
                foreach (var user in users)
                {
                    var identityUser = await _userManager.FindByIdAsync(user.UserId);

                    identityUser.EmailConfirmed = user.EmailConfirmed;
                    identityUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;

                    await _userManager.UpdateAsync(identityUser);
                }
            }

            return Json(await users.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Resends a user's email confirmation.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            var result = new ResendEmailConfirmResult();

            try
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = Id, code = code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                if (_emailSender.Response != null)
                {
                    if (_emailSender.Response.IsSuccessStatusCode)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Error = _emailSender.Response.Headers.ToString();
                    }
                }

            }
            catch (Exception e)
            {
                result.Error = e.Message;
            }

            return Json(result);
        }
    }
}
