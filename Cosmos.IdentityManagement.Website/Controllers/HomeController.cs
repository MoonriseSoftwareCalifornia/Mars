﻿using AspNetCore.Identity.Services.SendGrid;
using Cosmos.IdentityManagement.Website.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;

namespace Cosmos.IdentityManagement.Website.Controllers
{
    // See: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/areas?view=aspnetcore-3.1
    /// <summary>
    /// User management controller
    /// </summary>
    [Authorize(Roles = "User Administrators")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SendGridEmailSender _emailSender;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="emailSender"></param>
        public HomeController(
            ILogger<HomeController> logger,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = (SendGridEmailSender)emailSender;
        }

        /// <summary>
        ///     User manager home page
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Create a user
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        #region CREATE USER METHODS

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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

            var result = await CreateAccount(model);

            if (result.IdentityResult.Succeeded)
            {
                if (result.UserCreateViewModel == null)
                {
                    return View("UserCreated", null);
                }
                else
                {
                    return View("UserCreated", new UserCreatedViewModel(result.UserCreateViewModel, _emailSender.Response));
                }
            }

            foreach (var error in result.IdentityResult.Errors)
            {
                ModelState.AddModelError("", $"Code: {error.Code} Description: {error.Description}");
            }

            return View(model);
        }

        #endregion

        /// <summary>
        /// Creates a single user account
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isBatchJob">Email verifications work flows are different for users created in batch.</param>
        /// <returns></returns>
        private async Task<BulkUserCreatedResult> CreateAccount(UserCreateViewModel model, bool isBatchJob = false)
        {
            var result = new BulkUserCreatedResult();

            if (string.IsNullOrEmpty(model.Password) && model.GenerateRandomPassword == false)
            {
                ModelState.AddModelError("GenerateRandomPassword", "Must generate password if one not given.");
            }

            if (!ModelState.IsValid)
                return null;

            if (model.GenerateRandomPassword)
            {
                var password = new PasswordGenerator.Password();
                model.Password = password.Next();
            }

            var user = new IdentityUser()
            {
                Email = model.EmailAddress,
                EmailConfirmed = model.EmailConfirmed,
                NormalizedEmail = model.EmailAddress.ToUpperInvariant(),
                UserName = model.EmailAddress,
                NormalizedUserName = model.EmailAddress.ToUpperInvariant(),
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed
            };

            result.IdentityResult = await _userManager.CreateAsync(user, model.Password);

            if (result.IdentityResult.Succeeded)
            {
                if (model.EmailConfirmed)
                {
                    // Confirm email if set.
                    var emailCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var result2 = await _userManager.ConfirmEmailAsync(user,
                        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(emailCode)));
                }

                // Do we have to send an email confirmation or a reset password message?
                if (isBatchJob)
                {
                    // Always send a reset password email in this case
                    // For more information on how to enable account confirmation and password reset please
                    // visit https://go.microsoft.com/fwlink/?LinkID=532713
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);

                    var identityUser = await _userManager.GetUserAsync(User);

                    await _emailSender.SendEmailAsync(
                        user.Email,
                        "Create Password",
                        $"A new user account was created for you by {identityUser.Email}. Now we need you to create a password for your account by  <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    result.SendGridResponse = _emailSender.Response;
                    result.UserCreateViewModel = model;

                    if (!result.SendGridResponse.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", $"Could not send reset password email to: '{model.EmailAddress}'. Error: {result.SendGridResponse.Headers}");
                    }
                }
                else
                {
                    // Send an email confirmation if required.
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = "/" },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    result.SendGridResponse = _emailSender.Response;
                    result.UserCreateViewModel = model;

                    if (!result.SendGridResponse.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", $"Could not send email to: '{model.EmailAddress}'. Error: {result.SendGridResponse.Headers}");
                    }
                }

                return result;
            }

            foreach (var error in result.IdentityResult.Errors)
            {
                ModelState.AddModelError("", $"Code: {error.Code} Description: {error.Description}");
            }

            return null;
        }

        #region INDEX VIEW GRID METHODS

        /// <summary>
        /// Create users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task<IActionResult> Create_Users([DataSourceRequest] DataSourceRequest request, [Bind(Prefix = "models")] IEnumerable<UserIndexViewModel> models)
        {
            var results = new List<UserIndexViewModel>();

            if (models != null && ModelState.IsValid)
            {
                foreach (var user in models)
                {
                    _ = await CreateAccount(new UserCreateViewModel()
                    {
                        EmailAddress = user.EmailAddress,
                        EmailConfirmed = true,
                        PhoneNumber = user.PhoneNumber,
                        PhoneNumberConfirmed = user.PhoneNumberConfirmed
                    }, true);
                }
            }

            return Json(results.ToDataSourceResult(request, ModelState));
        }

        /// <summary>
        /// Updates users
        /// </summary>
        /// <param name="request"></param>
        /// <param name="users"></param>
        /// <returns></returns>
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
        /// <param name="id">Role ID</param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Users([DataSourceRequest] DataSourceRequest request, string id = "")
        {
            var users = new List<UserIndexViewModel>();

            try
            {

                if (string.IsNullOrEmpty(id))
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
                    var identityRole = await _roleManager.FindByIdAsync(id);

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
        public async Task<IActionResult> Update_Users([DataSourceRequest] DataSourceRequest request,
            [Bind(Prefix = "models")] IEnumerable<UserIndexViewModel> users)
        {
            if (users != null && ModelState.IsValid)
            {
                foreach (var user in users)
                {
                    var identityUser = await _userManager.FindByIdAsync(user.UserId);

                    identityUser.UserName = user.EmailAddress;
                    identityUser.NormalizedUserName = user.EmailAddress.ToUpperInvariant();
                    identityUser.Email = user.EmailAddress;
                    identityUser.NormalizedEmail = user.EmailAddress.ToUpperInvariant();
                    identityUser.EmailConfirmed = user.EmailConfirmed;
                    identityUser.PhoneNumberConfirmed = user.PhoneNumberConfirmed;

                    var result = await _userManager.UpdateAsync(identityUser);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", $"Error code: {error.Code} Error message: {error.Description}.");
                        }
                    }
                }
            }

            return Json(await users.ToDataSourceResultAsync(request, ModelState));
        }

        #endregion

        /// <summary>
        /// Gets the role assignments for a user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private async Task<UserRoleAssignmentsViewModel> GetRoleAssignmentsForUser(string id)
        {
            var roles = new List<IdentityRole>();
            var user = await _userManager.FindByIdAsync(id);
            var roleNames = await _userManager.GetRolesAsync(user);
            foreach (var name in roleNames)
            {
                roles.Add(await _roleManager.FindByNameAsync(name));
            }

            return new UserRoleAssignmentsViewModel()
            {
                Id = user.Id,
                Email = user.Email,
                IdentityRoles = roles
            };
        }

        /// <summary>
        /// Gets a total list of roles
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetRoles(string text)
        {
            var query = _roleManager.Roles.OrderBy(o => o.Name).Select(s => new
            {
                s.Id,
                s.Name
            }).AsQueryable();

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(s => s.Name.ToLower().StartsWith(text.ToLower()));
            }

            var roles = await query.ToListAsync();
            
            return Json(roles);
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

        /// <summary>
        /// Manages the role assignments for a user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> UserRoles(string id)
        {
            return View(await GetRoleAssignmentsForUser(id));
        }

        /// <summary>
        /// Updates a user's role assignments
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roleIds"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRoleAssignmentsViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            var roleNames = await _userManager.GetRolesAsync(user);

            // First, remove from all roles
            foreach (var name in roleNames)
            {
                if (name.Equals("User Administrators", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Make sure there is at least one administrator remaining
                    var administrators = await _userManager.GetUsersInRoleAsync("User Administrators");

                    if (administrators.Count() > 1)
                    {
                        await _userManager.RemoveFromRoleAsync(user, name);
                    }
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, name);
                }
            }

            var roles = new List<IdentityRole>();

            foreach(var roleId in model.RoleIds)
            {
                roles.Add(await _roleManager.FindByIdAsync(roleId));
            }

            // Now add back the new assignments
            foreach(var role in roles)
            {
                await _userManager.AddToRoleAsync(user, role.Name);
            }

            return View(await GetRoleAssignmentsForUser(model.Id));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
