using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserService _userService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<UserManagementController> _logger;
        private readonly NotificationService _notificationService;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            UserService userService,
            IWebHostEnvironment hostEnvironment,
            ILogger<UserManagementController> logger,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userService = userService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _notificationService = notificationService;
        }

        // GET: /UserManagement
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var roles = await _userManager.GetRolesAsync(user);

                // Prepare the view model
                var viewModel = new UserManagementViewModel
                {
                    FirstName = user.FirstName ?? "Admin",
                    Role = roles.FirstOrDefault() ?? "Admin",
                    NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                    CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    Users = new List<UserViewModel>(),
                    CurrentPage = page,
                    TotalPages = 1,
                    PageStartRecord = 0,
                    PageEndRecord = 0,
                    TotalRecords = 0,
                    Roles = await GetAllRolesAsync(),
                    PendingUsers = new List<PendingUserViewModel>(),
                    ActivityLogs = new List<ActivityLogViewModel>()
                };

                // Directly query users
                var users = await _userManager.Users
                    .OrderBy(u => u.Email)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                foreach (var u in users)
                {
                    var userRoles = await _userManager.GetRolesAsync(u);

                    viewModel.Users.Add(new UserViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Address = u.Address ?? "",
                        Role = userRoles.FirstOrDefault() ?? "User",
                        Status = u.Status ?? "Active",
                        MemberSince = u.MemberSince.ToString("MMM dd, yyyy"),
                        LastLogin = u.LastLoginDate.HasValue ? u.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never",
                        ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                        PropertyType = u.PropertyType ?? "Unknown",
                        OwnershipStatus = u.OwnershipStatus ?? "Unknown"
                    });
                }

                // Calculate pagination
                viewModel.TotalRecords = await _userManager.Users.CountAsync();
                viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalRecords / (double)pageSize);
                viewModel.PageStartRecord = viewModel.TotalRecords == 0 ? 0 : ((page - 1) * pageSize) + 1;
                viewModel.PageEndRecord = Math.Min(page * pageSize, viewModel.TotalRecords);

                return View("~/Views/UserManagement/AddUser.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the request.");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
            }

            {
                // Log the exception
                return View(new UserManagementViewModel
                {
                    FirstName = "Error",
                    Role = "Admin",
                    Users = new List<UserViewModel>(),
                    PendingUsers = new List<PendingUserViewModel>(),
                    Roles = new List<RoleViewModel>(),
                    ActivityLogs = new List<ActivityLogViewModel>()
                });
            }
        }

        private async Task<List<RoleViewModel>> GetAllRolesAsync()
        {
            var roles = _roleManager.Roles.ToList();
            var roleViewModels = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                if (role.Name != null) // Ensure role.Name is not null
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);

                    // Create a simplified permissions list
                    var permissions = new List<PermissionViewModel>
                    {
                        new PermissionViewModel
                        {
                            Id = "1",
                            Name = "View",
                            Category = "Basic",
                            IsGranted = true
                        }
                    };

                    roleViewModels.Add(new RoleViewModel
                    {
                        Name = role.Name,
                        UserCount = usersInRole.Count,
                        Permissions = permissions
                    });
                }
            }

            return roleViewModels;
        }

        // GET: /UserManagement/AddUser
        [HttpGet]
        public async Task<IActionResult> AddUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new UserFormViewModel
            {
                CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}",
                CurrentRole = roles.FirstOrDefault() ?? "Admin",
                CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" }
            };

            return View(viewModel);
        }

        // POST: /UserManagement/SaveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUser(UserFormViewModel model)
        {
            try
            {
                _logger.LogInformation($"Received form data: ID={model.Id}, Email={model.Email}, Role={model.Role}");

                // Clear validations for optional fields
                ModelState.Remove("Id");
                ModelState.Remove("ForcePasswordChange");
                ModelState.Remove("SendCredentials");
                ModelState.Remove("ProfileImage");
                ModelState.Remove("PropertyDocuments");
                ModelState.Remove("ResidentCount");

                _logger.LogInformation($"Received form with Role: {model.Role}");

                // Remove validations for role-specific fields
                if (!string.IsNullOrEmpty(model.Role))
                {
                    if (model.Role == "Staff")
                    {
                        ModelState.Remove("EmergencyContactName");
                        ModelState.Remove("EmergencyContactPhone");
                        ModelState.Remove("VehicleInfo");
                        ModelState.Remove("ResidentCount");
                        ModelState.Remove("MoveInDate");
                    }
                    else if (model.Role == "Homeowner")
                    {
                        ModelState.Remove("Department");
                        ModelState.Remove("Position");
                        ModelState.Remove("EmployeeId");
                    }
                    else
                    {
                        // For Admin, remove both sets of fields
                        ModelState.Remove("EmergencyContactName");
                        ModelState.Remove("EmergencyContactPhone");
                        ModelState.Remove("VehicleInfo");
                        ModelState.Remove("ResidentCount");
                        ModelState.Remove("MoveInDate");
                        ModelState.Remove("Department");
                        ModelState.Remove("Position");
                        ModelState.Remove("EmployeeId");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        var roles = await _userManager.GetRolesAsync(currentUser);
                        model.CurrentUserName = $"{currentUser.FirstName ?? ""} {currentUser.LastName ?? ""}";
                        model.CurrentRole = roles.FirstOrDefault() ?? "Admin";
                        model.CurrentUserProfileImageUrl = currentUser.ProfileImageUrl ?? "/images/default-avatar.png";
                        model.NotificationCount = await _notificationService.GetUnreadCountAsync(currentUser.Id);
                    }

                    model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };

                    TempData["ErrorMessage"] = "Please fix the validation errors.";
                    return View("AddUser", model);
                }

                // Process profile image if one was uploaded
                string? profileImageUrl = null;
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    profileImageUrl = await ProcessProfileImageUpload(model.ProfileImage);
                }

                // Get current user for activity logging
                var loggedInUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";

                // Check if we're editing an existing user or creating a new one
                if (!string.IsNullOrEmpty(model.Id))
                {
                    // UPDATE EXISTING USER
                    _logger.LogInformation("Updating existing user with ID: " + model.Id);

                    var existingUser = await _userManager.FindByIdAsync(model.Id);
                    if (existingUser == null)
                    {
                        TempData["ErrorMessage"] = "User not found.";
                        return RedirectToAction("UserManagement", "Dashboard");
                    }

                    // Update user properties
                    existingUser.FirstName = model.FirstName;
                    existingUser.LastName = model.LastName;
                    existingUser.Email = model.Email;
                    existingUser.UserName = model.Email; // Important: UserName needs to be updated too
                    existingUser.PhoneNumber = model.PhoneNumber;
                    existingUser.Address = model.Address ?? string.Empty;
                    existingUser.Unit = model.Unit ?? string.Empty;
                    existingUser.PropertyType = model.PropertyType ?? string.Empty;
                    existingUser.OwnershipStatus = model.OwnershipStatus ?? string.Empty;
                    existingUser.Department = model.Department ?? string.Empty;
                    existingUser.Position = model.Position ?? string.Empty;
                    existingUser.EmployeeId = model.EmployeeId ?? string.Empty;
                    existingUser.MoveInDate = model.MoveInDate;
                    existingUser.EmergencyContactName = model.EmergencyContactName ?? string.Empty;
                    existingUser.EmergencyContactPhone = model.EmergencyContactPhone ?? string.Empty;
                    existingUser.VehicleInfo = model.VehicleInfo ?? string.Empty; // Ensure not null
                    existingUser.ResidentCount = model.ResidentCount ?? 0;
                    existingUser.Status = model.Status;
                    existingUser.ReceiveEmailNotifications = model.ReceiveNotifications;
                    existingUser.ReceiveSmsNotifications = model.ReceiveSMS;
                    existingUser.Notes = model.Notes ?? string.Empty;
                    existingUser.DateOfBirth = model.DateOfBirth;
                    existingUser.ForcePasswordChange = model.ForcePasswordChange;

                    // Update profile image if a new one was uploaded
                    if (!string.IsNullOrEmpty(profileImageUrl))
                    {
                        existingUser.ProfileImageUrl = profileImageUrl;
                    }

                    // Update user in the database
                    var updateResult = await _userManager.UpdateAsync(existingUser);
                    if (updateResult.Succeeded)
                    {
                        // Update user's role if it has changed
                        var currentRoles = await _userManager.GetRolesAsync(existingUser);
                        var currentRole = currentRoles.FirstOrDefault();

                        if (currentRole != model.Role)
                        {
                            if (currentRole != null)
                            {
                                await _userManager.RemoveFromRoleAsync(existingUser, currentRole);
                            }
                            await _userManager.AddToRoleAsync(existingUser, model.Role);
                        }

                        // Update password if a new one was provided
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                            var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, model.Password);

                            if (!passwordResult.Succeeded)
                            {
                                foreach (var error in passwordResult.Errors)
                                {
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                            }
                        }

                        // Process property documents if any
                        if (model.PropertyDocuments != null && model.PropertyDocuments.Count > 0)
                        {
                            await ProcessPropertyDocumentsUpload(existingUser.Id, model.PropertyDocuments);
                        }

                        // Log user update
                        await _userService.LogActivityAsync(
                            loggedInUserId,
                            "user-updated",
                            $"Updated user: {existingUser.Email}",
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                        );

                        TempData["SuccessMessage"] = "User updated successfully.";
                        return RedirectToAction("UserManagement", "Dashboard");
                    }
                    else
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        TempData["ErrorMessage"] = "Failed to update user: " + string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    }
                }
                else
                {
                    // CREATE NEW USER
                    _logger.LogInformation("Creating new user with email: " + model.Email);

                    var newUser = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address ?? string.Empty,
                        Unit = model.Unit ?? string.Empty,
                        PropertyType = model.PropertyType ?? string.Empty,
                        OwnershipStatus = model.OwnershipStatus ?? string.Empty,
                        Department = model.Department ?? string.Empty,
                        Position = model.Position ?? string.Empty,
                        EmployeeId = model.EmployeeId ?? string.Empty,
                        MoveInDate = model.MoveInDate,
                        EmergencyContactName = model.Role != "Homeowner" ? "N/A" : (model.EmergencyContactName ?? "N/A"),
                        EmergencyContactPhone = model.Role != "Homeowner" ? "N/A" : (model.EmergencyContactPhone ?? "N/A"),
                        VehicleInfo = model.VehicleInfo ?? string.Empty, // Ensure not null
                        ResidentCount = model.ResidentCount ?? 0,
                        Status = model.Status,
                        MemberSince = DateTime.Now,
                        ProfileImageUrl = profileImageUrl ?? "/images/default-avatar.png",
                        ReceiveEmailNotifications = model.ReceiveNotifications,
                        ReceiveSmsNotifications = model.ReceiveSMS,
                        Notes = model.Notes ?? string.Empty,
                        ForcePasswordChange = model.ForcePasswordChange,
                        DateOfBirth = model.DateOfBirth
                    };

                    var result = await _userManager.CreateAsync(newUser, model.Password);

                    if (result.Succeeded)
                    {
                        // Assign role
                        await _userManager.AddToRoleAsync(newUser, model.Role);

                        // Process property documents if any
                        if (model.PropertyDocuments != null && model.PropertyDocuments.Count > 0)
                        {
                            await ProcessPropertyDocumentsUpload(newUser.Id, model.PropertyDocuments);
                        }

                        // Send credentials email if requested
                        if (model.SendCredentials)
                        {
                            await SendAccountCredentialsEmail(newUser, model.Password);
                        }

                        // Log user creation
                        await _userService.LogActivityAsync(
                            loggedInUserId,
                            "user-created",
                            $"Created new user: {newUser.Email}",
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                        );

                        TempData["SuccessMessage"] = "User created successfully.";
                        return RedirectToAction("UserManagement", "Dashboard");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }

                        TempData["ErrorMessage"] = "Failed to create user. See error details below.";
                    }
                }

                // Populate the model with current user information for displaying the page again
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    model.CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}";
                    model.CurrentRole = userRoles.FirstOrDefault() ?? "Admin";
                    model.CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                    model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                }

                model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };
                return View("AddUser", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;

                // Populate the model with current user information for displaying the page again
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    model.CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}";
                    model.CurrentRole = userRoles.FirstOrDefault() ?? "Admin";
                    model.CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                    model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                }

                model.AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" };
                return View("AddUser", model);
            }
        }

        // GET: /UserManagement/EditUser/5
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userToEdit = await _userManager.FindByIdAsync(id);
            if (userToEdit == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("UserManagement", "Dashboard");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRoles = await _userManager.GetRolesAsync(userToEdit);

            var viewModel = new UserFormViewModel
            {
                Id = userToEdit.Id,
                FirstName = userToEdit.FirstName,
                LastName = userToEdit.LastName,
                Email = userToEdit.Email ?? string.Empty,
                PhoneNumber = userToEdit.PhoneNumber ?? string.Empty,
                Address = userToEdit.Address ?? string.Empty,
                Unit = userToEdit.Unit ?? string.Empty,
                PropertyType = userToEdit.PropertyType ?? string.Empty,
                OwnershipStatus = userToEdit.OwnershipStatus ?? string.Empty,
                Role = userRoles.FirstOrDefault() ?? "Homeowner",
                Status = userToEdit.Status ?? "Pending",
                Department = userToEdit.Department ?? string.Empty,
                Position = userToEdit.Position ?? string.Empty,
                EmployeeId = userToEdit.EmployeeId ?? string.Empty,
                MoveInDate = userToEdit.MoveInDate,
                EmergencyContactName = userToEdit.EmergencyContactName ?? string.Empty,
                EmergencyContactPhone = userToEdit.EmergencyContactPhone ?? string.Empty,
                VehicleInfo = userToEdit.VehicleInfo ?? string.Empty,
                ResidentCount = userToEdit.ResidentCount,
                DateOfBirth = userToEdit.DateOfBirth,
                Notes = userToEdit.Notes ?? string.Empty,
                ReceiveNotifications = userToEdit.ReceiveEmailNotifications.HasValue ? userToEdit.ReceiveEmailNotifications.Value : true,
                ReceiveSMS = userToEdit.ReceiveSmsNotifications.HasValue ? userToEdit.ReceiveSmsNotifications.Value : false,
                CurrentUserName = $"{user.FirstName ?? ""} {user.LastName ?? ""}",
                CurrentRole = roles.FirstOrDefault() ?? "Admin",
                CurrentUserProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                AvailableRoles = new List<string> { "Admin", "Staff", "Homeowner" },
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return View("AddUser", viewModel);
        }

        // POST: /UserManagement/ResetPassword

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword, bool forcePasswordChange, bool sendEmail)
        {
            try
            {
                _logger.LogInformation($"Password reset requested for user ID: {userId}");

                // Ensure userId is not null/empty
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "User ID is required.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }

                // Generate token and reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (result.Succeeded)
                {
                    // Update force password change flag if needed
                    if (forcePasswordChange)
                    {
                        user.ForcePasswordChange = true;

                        // Ensure VehicleInfo is not null
                        if (user.VehicleInfo == null)
                        {
                            user.VehicleInfo = string.Empty;
                        }

                        await _userManager.UpdateAsync(user);
                    }

                    // Log the password reset
                    await _userService.LogActivityAsync(
                        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                        "password-reset",
                        $"Password reset for user {user.Email}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    );

                    // Send email if requested
                    if (sendEmail)
                    {
                        _logger.LogInformation($"Email notification for password reset would be sent to {user.Email}");
                        await SendPasswordResetEmail(user, newPassword);
                    }

                    _logger.LogInformation($"Password reset successful for user {user.Email}");
                    TempData["SuccessMessage"] = "Password reset successfully.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }
                else
                {
                    // Log specific errors
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Password reset error for {userId}: {error.Description}");
                    }

                    TempData["ErrorMessage"] = $"Failed to reset password: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    return RedirectToAction("UserManagement", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for user {userId}");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("UserManagement", "Dashboard");
            }
        }

        // POST: /UserManagement/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                _logger.LogInformation($"Attempting to delete user with ID: {id}");

                // Validate ID
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Delete user attempt with empty ID");
                    TempData["ErrorMessage"] = "Invalid user ID provided.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {id}");
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }

                // Check if this is the last admin
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count <= 1)
                    {
                        _logger.LogWarning("Attempt to delete the last admin user prevented");
                        TempData["ErrorMessage"] = "Cannot delete the last admin user.";
                        return RedirectToAction("UserManagement", "Dashboard");
                    }
                }

                // Log activity before deleting the user
                await _userService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system",
                    "user-deleted",
                    $"Deleted user: {user.Email}",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                );

                // Now delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Successfully deleted user {id}");
                    TempData["SuccessMessage"] = "User deleted successfully.";
                    return RedirectToAction("UserManagement", "Dashboard");
                }
                else
                {
                    _logger.LogError($"Failed to delete user {id}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    TempData["ErrorMessage"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("UserManagement", "Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("UserManagement", "Dashboard");
            }
        }

        // POST: /UserManagement/ChangeUserStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserStatus(string userId, string status)
        {
            try
            {
                _logger.LogInformation($"Attempting to change status for user {userId} to {status}");

                if (string.IsNullOrEmpty(status))
                {
                    return Json(new { success = false, message = "Status cannot be empty." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    return Json(new { success = false, message = "User not found." });
                }

                // Check if trying to suspend or deactivate the last admin
                if ((status == "Suspended" || status == "Inactive") && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                    if (adminUsers.Count <= 1)
                    {
                        _logger.LogWarning("Attempt to change status of the last admin user prevented");
                        return Json(new { success = false, message = "Cannot modify status of the last admin user." });
                    }
                }

                user.Status = status;

                // Ensure VehicleInfo is not null
                if (user.VehicleInfo == null)
                {
                    user.VehicleInfo = string.Empty;
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Log status change using the user service
                    var loggedInUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";

                    await _userService.LogActivityAsync(
                        loggedInUserId,
                        "status-change",
                        $"Changed status of user {user.Email} to {status}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                    );

                    _logger.LogInformation($"Successfully changed status for user {userId} to {status}");
                    return Json(new { success = true, message = $"User status changed to {status} successfully." });
                }
                else
                {
                    _logger.LogError($"Failed to update user status: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return Json(new { success = false, message = "Failed to update user status." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing user status for user {userId}");
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        #region Helper Methods

        private async Task<string> ProcessProfileImageUpload(IFormFile profileImage)
        {
            if (profileImage == null || profileImage.Length == 0)
                return string.Empty;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(profileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new Exception("Only JPG, JPEG, and PNG files are allowed for profile images.");
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
                await profileImage.CopyToAsync(fileStream);
            }

            return $"/{userFolderRelative}/{uniqueFileName}";
        }

        private async Task ProcessPropertyDocumentsUpload(string userId, List<IFormFile> documents)
        {
            if (documents == null || !documents.Any())
                return;

            string docFolderRelative = $"documents/users/{userId}";
            string docFolderAbsolute = Path.Combine(_hostEnvironment.WebRootPath, docFolderRelative);

            if (!Directory.Exists(docFolderAbsolute))
            {
                Directory.CreateDirectory(docFolderAbsolute);
            }

            foreach (var document in documents)
            {
                if (document.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(document.FileName);
                    string filePath = Path.Combine(docFolderAbsolute, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await document.CopyToAsync(fileStream);
                    }

                    // In a real implementation, you'd save document metadata to the database
                    // await _documentService.SaveDocument(userId, document.FileName, uniqueFileName, filePath);
                }
            }
        }

        private async Task SendAccountCredentialsEmail(ApplicationUser user, string password)
        {
            if (user == null)
            {
                _logger.LogError("Cannot send credentials email to null user");
                return;
            }

            // In a real implementation, you would use an email service to send emails
            _logger.LogInformation($"Sending credentials email to {user.Email}");

            // Example of what would be sent
            var emailBody = $@"
            Hello {user.FirstName},

            Your account has been created in the Green Meadows Portal.

            Username: {user.Email}
            Password: {password}

            Please log in at https://yourportal.com/ and change your password as soon as possible.

            Regards,
            Green Meadows Management
            ";

            // Placeholder for actual email sending
            // await _emailService.SendEmail(user.Email, "Your Green Meadows Portal Account", emailBody);
            await Task.CompletedTask;
        }

        private async Task SendPasswordResetEmail(ApplicationUser user, string newPassword)
        {
            if (user == null)
            {
                _logger.LogError("Cannot send password reset email to null user");
                return;
            }

            // In a real implementation, you would use an email service to send emails
            _logger.LogInformation($"Sending password reset email to {user.Email}");

            // Example of what would be sent
            var emailBody = $@"
            Hello {user.FirstName},

            Your password has been reset by an administrator.

            Username: {user.Email}
            New Password: {newPassword}

            Please log in at https://yourportal.com/ and change your password as soon as possible.

            Regards,
            Green Meadows Management
            ";

            // Placeholder for actual email sending
            // await _emailService.SendEmail(user.Email, "Green Meadows Portal - Password Reset", emailBody);
            await Task.CompletedTask;
        }

        #endregion
    }
}