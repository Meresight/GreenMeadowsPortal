using Azure;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GreenMeadowsPortal.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment hostEnvironment,
        ILogger<AccountController> logger) : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
        private readonly ILogger<AccountController> _logger = logger;

        private void DisableBackHistory()
        {
            Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
            Response.Headers.XContentTypeOptions = "nosniff";
        }

        [HttpGet]
        public IActionResult Login()
        {
            DisableBackHistory();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                    if (result.Succeeded)
                    {
                        // Update last login date
                        user.LastLoginDate = DateTime.Now;
                        await _userManager.UpdateAsync(user);

                        // Log successful login for debugging
                        _logger.LogInformation($"User {user.Email} logged in at {DateTime.Now}");

                        return RedirectToAction("RedirectUserBasedOnRole", new { userId = user.Id });
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> AdminProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var profileModel = new ProfileViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-profile.png",
                MemberSince = user.MemberSince.ToString("MMMM yyyy"),
                Status = user.Status ?? "Active",
                Role = roles.FirstOrDefault() ?? "User"
            };

            return View(profileModel);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    Status = "Pending"  // Set the status here

                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }

                    await _userManager.AddToRoleAsync(user, model.Role);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("RedirectUserBasedOnRole", new { userId = user.Id });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Identity.Application");

            DisableBackHistory();
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> RedirectUserBasedOnRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("AdminDashboard", "Dashboard");
            if (roles.Contains("Staff"))
                return RedirectToAction("StaffDashboard", "Dashboard");

            return RedirectToAction("HomeownerDashboard", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);
            var profileModel = new ProfileViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-profile.png",
                MemberSince = user.MemberSince.ToString("MMMM yyyy"),
                Status = user.Status ?? "Active",
                Role = roles.FirstOrDefault() ?? "User"
            };

            if (profileModel == null)
            {
                TempData["ErrorMessage"] = "Failed to load profile data.";
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Dashboard/Profile.cshtml", profileModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var roles = await _userManager.GetRolesAsync(user);

            ModelState.Remove("Role");
            ModelState.Remove("Status");
            ModelState.Remove("ProfileImageUrl");
            ModelState.Remove("MemberSince");

            model.Role = roles.FirstOrDefault() ?? "User";
            model.Status = user.Status ?? "Active";
            model.MemberSince = user.MemberSince.ToString("MMMM yyyy");

            if (model.ProfileImage == null)
            {
                model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-profile.png";
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                TempData["ErrorMessage"] = "There was a problem updating your profile.";
                return View("~/Views/Dashboard/Profile.cshtml", model);
            }

            bool isUpdated = false;

            var nameParts = model.FullName?.Split(' ', 2);
            string firstName = nameParts?.FirstOrDefault() ?? "";
            string lastName = nameParts?.Skip(1).FirstOrDefault() ?? "";

            if (user.FirstName != firstName) { user.FirstName = firstName; isUpdated = true; }
            if (user.LastName != lastName) { user.LastName = lastName; isUpdated = true; }
            if (user.PhoneNumber != model.PhoneNumber) { user.PhoneNumber = model.PhoneNumber; isUpdated = true; }
            if (user.Address != model.Address) { user.Address = model.Address; isUpdated = true; }

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.ProfileImage.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(string.Empty, "Only JPG, JPEG, and PNG files are allowed.");
                    return View("~/Views/Dashboard/Profile.cshtml", model);
                }

                string userFolderRelative = "images/users";
                string userFolderAbsolute = Path.Combine(_hostEnvironment.WebRootPath, userFolderRelative);

                if (!Directory.Exists(userFolderAbsolute))
                {
                    Directory.CreateDirectory(userFolderAbsolute);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                string filePath = Path.Combine(userFolderAbsolute, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(fileStream);
                }

                user.ProfileImageUrl = $"/{userFolderRelative}/{uniqueFileName}";
                isUpdated = true;
            }

            if (isUpdated)
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                TempData["InfoMessage"] = "No changes were detected.";
            }

            return View("~/Views/Dashboard/Profile.cshtml", model);
        }
    }
}
