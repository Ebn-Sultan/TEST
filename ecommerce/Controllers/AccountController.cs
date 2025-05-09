using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.ViewModel;
using ecommerce.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;

namespace ecommerce.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IRepository<Shipment> shipmentRepository;
        private readonly IOrderItemRepository orderItemRepository;
        private readonly IProductRepository productRepository;

        public AccountController(UserManager<ApplicationUser> _userManager,
            SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager,
            IRepository<Shipment> _Repository, IOrderItemRepository _orderItemRepository,
            IProductRepository _productRepository)
        {
            userManager = _userManager;
            signInManager = _signInManager;
            roleManager = _roleManager;
            shipmentRepository = _Repository;
            orderItemRepository = _orderItemRepository;
            productRepository = _productRepository;
        }

        public IActionResult Index()
        {
      
            return View();
        }

        [HttpGet]
        public IActionResult register(bool IsAdmin = false)
        {
            ViewBag.IsAdmin = IsAdmin;
            return View("register");
        }

        [HttpGet]
        public async Task<IActionResult> mailConfirmed(string email)
        {
            ApplicationUser? user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return RedirectToAction("login");
            }
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
            return RedirectToAction("login");
        }

        [HttpPost]
        public async Task<IActionResult> register(RegisterViewModel model, bool isAdmin)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser applicationUser = new ApplicationUser
                {
                    UserName = model.userName,
                    PhoneNumber = model.phoneNumber,
                    Email = model.Email?.Trim()
                };

                IdentityResult result = await userManager.CreateAsync(applicationUser, model.password);
                if (result.Succeeded)
                {
                    // Ensure roles exist
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                    }
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    // Add user to role based on isAdmin
                    if (isAdmin)
                    {
                        await userManager.AddToRoleAsync(applicationUser, "Admin");
                        return RedirectToAction("admins", "dashbourd");
                    }
                    else
                    {
                        await userManager.AddToRoleAsync(applicationUser, "User");
                        return RedirectToAction("SendForceEmailConfirmationMail", "Mail", new { toEmail = model.Email });
                    }
                }

                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail")
                    {
                        ModelState.AddModelError(string.Empty, "This email or username is already registered.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            ViewBag.IsAdmin = isAdmin;
            return View("register", model);
        }

        [HttpGet]
        public IActionResult login()
        {
            return View("login");
        }

        // omar : saeed take a look at what happens when the user enters a wrong passwprd at login
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByNameAsync(model.userName);
                if (user != null)
                {
                    bool matched = await userManager.CheckPasswordAsync(user, model.password);
                    if (matched)
                    {
                        if (user.EmailConfirmed)
                        {
                            List<Claim> claims = new List<Claim>();
                            claims.Add(new Claim("name", model.userName));
                            await signInManager.SignInWithClaimsAsync(user, model.rememberMe, claims);
                            return RedirectToAction("Index", "Home");
                        }
                        ModelState.AddModelError("", "Unconfirmed email");
                        return View("login", model);
                    }
                    ModelState.AddModelError("", "invalid password");
                    return View("login", model);
                }
                ModelState.AddModelError("", "invalid user name");
                return View("login", model);
            }
            return View("login", model);
        }

        public async Task<IActionResult> logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("login");
        }

        public async Task<IActionResult> confirmMakeAdmin(string userName)
        {
            ApplicationUser user = await userManager.FindByNameAsync(userName);
            if (user != null)
            {
                await userManager.RemoveFromRoleAsync(user, "User");
                await userManager.AddToRoleAsync(user, "Admin");
                return RedirectToAction("users", "Dashbourd");
            }
            return RedirectToAction("users", "Dashbourd");
        }

        public async Task<IActionResult> confirmRemoveAdmin(string userName)
        {
            ApplicationUser appUser = await userManager.FindByNameAsync(userName);
            if (appUser != null)
            {
                await userManager.RemoveFromRoleAsync(appUser, "Admin");
                await userManager.AddToRoleAsync(appUser, "User");
                if (User.FindFirst("name")?.Value == userName)
                {
                    return RedirectToAction("logout");
                }
                return RedirectToAction("admins", "dashbourd");
            }
            return RedirectToAction("admins", "dashbourd"); // which view should be returned if user not found!!!!!
        }

        [HttpGet]
        public IActionResult forgotPassword()
        {
            return View("forgotPassword");
        }

        [HttpPost]
        public async Task<IActionResult> forgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Email = model?.Email.Trim();
                ApplicationUser? user = await userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    string token = await userManager.GeneratePasswordResetTokenAsync(user);
                    string callBackUrl = Url.Action("resetPassword", "account", values: new { token, userName = user.UserName },
                        protocol: Request.Scheme);

                    return RedirectToAction("SendMail", "Mail",
                        routeValues: new { emailTo = user.Email, username = user.UserName, callBackUrl = callBackUrl });
                }
                ModelState.AddModelError("", "Email not existed");
                return View("forgotPassword", model);
            }
            return View("forgotPassword", model);
        }

        [HttpGet]
        public IActionResult resetPassword([FromQueryAttribute] string userName, [FromQueryAttribute] string token)
        {
            ViewBag.UserName = userName;
            ViewBag.Token = token;
            return View("resetPassword");
        }

        [HttpPost]
        public async Task<IActionResult> resetPassword(resetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser? user = await userManager.FindByNameAsync(model.userName);
                if (user != null)
                {
                    IdentityResult result = await userManager.ResetPasswordAsync(user, model.token, model.newPassword);
                    if (result.Succeeded)
                    {
                        return View("login");
                    }
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View("resetPassword", model);
                }
                ModelState.AddModelError("", "User not found");
                return View("resetPassword", model);
            }
            return View("resetPassword", model);
        }

        public async Task<IActionResult> myAccount(string selectedPartial = "_accountInfoPartial", resetPasswordViewModel changePasswordModel = null)
        {
            ApplicationUser user = await userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("login");
            }
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                Email = user.Email
            };
            ViewBag.selectedPartial = selectedPartial;
            ViewBag.resetPasswordModel = changePasswordModel;
            return View(model);
        }

        public async Task<IActionResult> getAccountInfoPartial()
        {
            ApplicationUser user = await userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("login");
            }
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                Email = user.Email
            };
            return View("_accountInfoPartial", model);
        }

        public IActionResult getAccountChangePasswordPartial()
        {
            return View("_accountChangePasswordPartial");
        }

        public IActionResult getAccountOrdersPartial()
        {
            return View("_accountOrdersPartial");
        }

        public async Task<IActionResult> getAccountShipmentsPartial()
        {
            ApplicationUser? user = await userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("login");
            }
            List<Shipment>? shipments = shipmentRepository.Get(s => s.UserId == user.Id);
            return View("_accountShipmentsPartial", shipments);
        }

        [HttpPost]
        public async Task<IActionResult> editAccountInfo(string userName, string phoneNumber, string Email)
        {
            AccountInfoViewModel model = new AccountInfoViewModel()
            {
                userName = userName,
                phoneNumber = phoneNumber,
                Email = Email
            };

            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByEmailAsync(Email);
                if (user != null)
                {
                    ApplicationUser existingUser = await userManager.FindByNameAsync(model.userName);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("userName", "This username is already taken.");
                        return RedirectToAction("myAccount", new { model });
                    }

                    user.UserName = model.userName;
                    user.PhoneNumber = model.phoneNumber;
                    IdentityResult result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        if (User.Identity.Name != user.UserName)
                        {
                            return RedirectToAction("logout");
                        }
                        return RedirectToAction("Index", "Home");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return RedirectToAction("myAccount", new { model });
                }
                ModelState.AddModelError("invalidSentEmail", "User not found");
                return RedirectToAction("myAccount", new { model });
            }
            return RedirectToAction("myAccount", new { model });
        }

        public async Task<IActionResult> editAccountPassword(resetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = await userManager.FindByNameAsync(User.Identity?.Name);
                if (user != null)
                {
                    string token = await userManager.GeneratePasswordResetTokenAsync(user);
                    IdentityResult result = await userManager.ResetPasswordAsync(user, token, model.newPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return RedirectToAction("myAccount", new { selectedPartial = "_accountChangePasswordPartial", changePasswordModel = model });
                }
                ModelState.AddModelError("", "Invalid user");
                return RedirectToAction("myAccount", new { selectedPartial = "_accountChangePasswordPartial", changePasswordModel = model });
            }
            return RedirectToAction("myAccount", new { selectedPartial = "_accountChangePasswordPartial", changePasswordModel = model });
        }
    }
}