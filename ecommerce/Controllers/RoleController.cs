using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ecommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ILogger<RoleController> _logger;

        public RoleController(RoleManager<IdentityRole> roleManager, ILogger<RoleController> logger)
        {
            this.roleManager = roleManager;
            this._logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> InitializeRoles()
        {
            try
            {
                string[] roleNames = { "Admin", "User" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var role = new IdentityRole { Name = roleName };
                        var result = await roleManager.CreateAsync(role);
                        if (result.Succeeded)
                        {
                            _logger.LogInformation("Role created successfully: {RoleName}", roleName);
                        }
                        else
                        {
                            _logger.LogError("Failed to create role: {RoleName}, Errors: {Errors}",
                                roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                            ModelState.AddModelError("", $"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                            return View("Error");
                        }
                    }
                }
                return RedirectToAction("Index", "Dashbourd");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while initializing roles");
                ModelState.AddModelError("", "An unexpected error occurred while initializing roles.");
                return View("Error");
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var roles = roleManager.Roles.ToList();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                _logger.LogWarning("Role not found for deletion: {RoleId}", id);
                return RedirectToAction("GetAll");
            }
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(IdentityRole role)
        {
            try
            {
                var existingRole = await roleManager.FindByIdAsync(role.Id);
                if (existingRole == null)
                {
                    _logger.LogWarning("Role not found for deletion: {RoleId}", role.Id);
                    return RedirectToAction("GetAll");
                }

                var result = await roleManager.DeleteAsync(existingRole);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Role deleted successfully: {RoleName}", existingRole.Name);
                    return RedirectToAction("GetAll");
                }

                _logger.LogError("Failed to delete role: {RoleName}, Errors: {Errors}",
                    existingRole.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                ModelState.AddModelError("", $"Failed to delete role {existingRole.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return View(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting role: {RoleId}", role.Id);
                ModelState.AddModelError("", "An unexpected error occurred while deleting the role.");
                return View(role);
            }
        }
    }
}