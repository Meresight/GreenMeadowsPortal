using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserService _userService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            UserService userService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userService = userService;
        }

        // User Management action methods
        [HttpGet]
        public async Task<IActionResult> UserManagement(int page = 1, string tab = "all-users", int logPage = 1)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(admin);

            // Create the view model
            var viewModel = new UserManagementViewModel
            {
                FirstName = admin.FirstName,
                Role = roles.FirstOrDefault() ?? "Admin",
                NotificationCount = 3, // You would get this from a notification service
                CurrentPage = page,
                LogsCurrentPage = logPage,
                Users = await GetPaginatedUsers(page, 10),
                PendingUsers = await GetPendingUsers(),
                Roles = await GetSystemRoles(),
                ActivityLogs = await GetActivityLogs(logPage, 10)
            };

            // Calculate pagination values
            viewModel.TotalRecords = await GetTotalUserCount();
            viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalRecords / 10.0);
            viewModel.PageStartRecord = ((page - 1) * 10) + 1;
            viewModel.PageEndRecord = Math.Min(page * 10, viewModel.TotalRecords);

            // For activity logs pagination
            viewModel.TotalLogs = await GetTotalLogsCount();
            viewModel.LogsTotalPages = (int)Math.Ceiling(viewModel.TotalLogs / 10.0);
            viewModel.LogsPageStartRecord = ((logPage - 1) * 10) + 1;
            viewModel.LogsPageEndRecord = Math.Min(logPage * 10, viewModel.TotalLogs);

            // Pending request count
            viewModel.PendingRequestsCount = viewModel.PendingUsers.Count;

            return View(viewModel);
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
        [HttpPost]
        public async Task<IActionResult> SaveUser(UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return RedirectToAction("UserManagement");
            }

            // Check if this is an edit or create operation
            if (!string.IsNullOrEmpty(model.Id)) // Fix: Replaced 'user.Id' with 'model.Id'
            {
                // Update existing user
                var user = await _userManager.FindByIdAsync(model.Id.ToString());
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("UserManagement");
                }

                // Update user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;

                // Update user role if needed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Update status
                user.Status = model.Status;

                // Save changes
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update user.";
                }
            }
            else
            {
                // Create new user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Status = model.Status,
                    MemberSince = DateTime.Now
                };

                // Create temporary password or use the one provided
                var password = model.Password ?? GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign role
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // Send email with credentials if requested
                    if (model.SendCredentials)
                    {
                        // Implementation for sending email would go here
                    }

                    TempData["SuccessMessage"] = "User created successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create user.";
                }
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int userId, string notes)
        {
            // Implementation to approve a pending user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.Status = "Active";
                await _userManager.UpdateAsync(user);

                // Send approval notification to user
                // Implementation for sending email would go here

                TempData["SuccessMessage"] = "User approved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(int userId, string notes)
        {
            // Implementation to reject a pending user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                user.Status = "Rejected";
                await _userManager.UpdateAsync(user);

                // Send rejection notification to user
                // Implementation for sending email would go here

                TempData["SuccessMessage"] = "User registration rejected.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found.";
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int userId, string newPassword, bool forcePasswordChange, bool sendEmail)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("UserManagement");
            }

            // Reset the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // Set force password change flag if needed
                if (forcePasswordChange)
                {
                    // You would need to implement this in your ApplicationUser model
                    user.ForcePasswordChange = true;
                    await _userManager.UpdateAsync(user);
                }

                // Send email with new password if requested
                if (sendEmail)
                {
                    // Implementation for sending email would go here
                }

                TempData["SuccessMessage"] = "Password reset successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reset password.";
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole(string roleName, string description, string originalRoleName, List<string> permissions)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                TempData["ErrorMessage"] = "Role name is required.";
                return RedirectToAction("UserManagement");
            }

            // Check if this is an edit or create operation
            if (!string.IsNullOrEmpty(originalRoleName))
            {
                // Update existing role
                var role = await _roleManager.FindByNameAsync(originalRoleName);
                if (role == null)
                {
                    TempData["ErrorMessage"] = "Role not found.";
                    return RedirectToAction("UserManagement");
                }

                // Rename role if needed
                if (originalRoleName != roleName)
                {
                    role.Name = roleName;
                    await _roleManager.UpdateAsync(role);
                }

                // Update permissions for this role
                // This would depend on how you're storing permissions
                // You might use claims or a separate permissions table

                TempData["SuccessMessage"] = "Role updated successfully.";
            }
            else
            {
                // Create new role
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    // Assign permissions to this role
                    // This would depend on how you're storing permissions

                    TempData["SuccessMessage"] = "Role created successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create role.";
                }
            }

            return RedirectToAction("UserManagement");
        }

        // Helper methods
        private async Task<List<UserViewModel>> GetPaginatedUsers(int page, int pageSize)
        {
            // Implementation to get paginated users from the database
            // This would interact with your user repository or directly with UserManager

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            // Placeholder implementation
            var users = new List<UserViewModel>();
            // ... populate with actual user data

            return users;
        }

        private async Task<List<PendingUserViewModel>> GetPendingUsers()
        {
            // Implementation to get pending users from the database

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            // Placeholder implementation
            var pendingUsers = new List<PendingUserViewModel>();
            // ... populate with actual pending user data

            return pendingUsers;
        }

        private async Task<List<RoleViewModel>> GetSystemRoles()
        {
            // Implementation to get all roles from the database

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            // Placeholder implementation
            var roles = new List<RoleViewModel>();
            // ... populate with actual role data

            return roles;
        }

        private async Task<List<ActivityLogViewModel>> GetActivityLogs(int page, int pageSize)
        {
            // Implementation to get paginated activity logs from the database

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            // Placeholder implementation
            var logs = new List<ActivityLogViewModel>();
            // ... populate with actual log data

            return logs;
        }

        private async Task<int> GetTotalUserCount()
        {
            // Implementation to get total user count from the database

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            return 100; // Placeholder value
        }

        private async Task<int> GetTotalLogsCount()
        {
            // Implementation to get total logs count from the database

            // Add this line to make the method truly asynchronous
            await Task.CompletedTask;

            return 50; // Placeholder value
        }

        private string GenerateRandomPassword()
        {
            // Generate a random password that meets requirements
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 10)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return result;
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserStatus(int userId, string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                return Json(new { success = false, message = "Status cannot be empty." });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            // Update user status
            user.Status = status;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Log activity
                await LogUserActivity(user.Id, "status-change", $"User status changed to {status}");

                return Json(new { success = true, message = $"User status changed to {status}." });
            }

            return Json(new { success = false, message = "Failed to update user status." });
        }

        [HttpPost]
        public async Task<IActionResult> BulkAction(string action, List<int> userIds)
        {
            if (string.IsNullOrEmpty(action) || userIds == null || !userIds.Any())
            {
                return Json(new { success = false, message = "Invalid request parameters." });
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    failCount++;
                    continue;
                }

                bool success = false;

                switch (action.ToLower())
                {
                    case "activate":
                        user.Status = "Active";
                        var activateResult = await _userManager.UpdateAsync(user);
                        success = activateResult.Succeeded;
                        if (success)
                        {
                            await LogUserActivity(user.Id, "status-change", "User activated via bulk action");
                        }
                        break;

                    case "suspend":
                        user.Status = "Suspended";
                        var suspendResult = await _userManager.UpdateAsync(user);
                        success = suspendResult.Succeeded;
                        if (success)
                        {
                            await LogUserActivity(user.Id, "status-change", "User suspended via bulk action");
                        }
                        break;

                    case "deactivate":
                        user.Status = "Inactive";
                        var deactivateResult = await _userManager.UpdateAsync(user);
                        success = deactivateResult.Succeeded;
                        if (success)
                        {
                            await LogUserActivity(user.Id, "status-change", "User deactivated via bulk action");
                        }
                        break;

                    case "delete":
                        var deleteResult = await _userManager.DeleteAsync(user);
                        success = deleteResult.Succeeded;
                        if (success)
                        {
                            await LogUserActivity(user.Id, "account-deleted", "User deleted via bulk action");
                        }
                        break;

                    case "export":
                        // Handle export separately
                        success = true;
                        break;
                }

                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            if (action.ToLower() == "export")
            {
                // For export, we would typically handle this differently
                // but for now, we'll just return a success message
                return Json(new { success = true, message = $"Data for {userIds.Count} users prepared for export." });
            }

            if (successCount > 0 && failCount == 0)
            {
                return Json(new { success = true, message = $"Successfully applied {action} to {successCount} users." });
            }
            else if (successCount > 0 && failCount > 0)
            {
                return Json(new { success = true, message = $"Applied {action} to {successCount} users. Failed for {failCount} users." });
            }
            else
            {
                return Json(new { success = false, message = $"Failed to apply {action} to any users." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRolePermissions(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return Json(new { success = false, message = "Role name cannot be empty." });
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return Json(new { success = false, message = "Role not found." });
            }

            // Get users in this role
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            var userCount = usersInRole.Count;

            // Get permissions for this role
            // This would depend on how you're storing permissions
            // Here's a simplified approach
            var permissionCategories = new List<object>
            {
                new
                {
                    categoryName = "User Management",
                    permissions = new List<object>
                    {
                        new { permissionName = "View Users", granted = true },
                        new { permissionName = "Create Users", granted = roleName == "Admin" },
                        new { permissionName = "Edit Users", granted = roleName == "Admin" },
                        new { permissionName = "Delete Users", granted = roleName == "Admin" },
                        new { permissionName = "Manage Roles", granted = roleName == "Admin" }
                    }
                },
                new
                {
                    categoryName = "Announcements",
                    permissions = new List<object>
                    {
                        new { permissionName = "View Announcements", granted = true },
                        new { permissionName = "Create Announcements", granted = roleName == "Admin" || roleName == "Staff" },
                        new { permissionName = "Edit Announcements", granted = roleName == "Admin" || roleName == "Staff" },
                        new { permissionName = "Delete Announcements", granted = roleName == "Admin" }
                    }
                },
                new
                {
                    categoryName = "Billing",
                    permissions = new List<object>
                    {
                        new { permissionName = "View Billing", granted = true },
                        new { permissionName = "Create Invoices", granted = roleName == "Admin" },
                        new { permissionName = "Process Payments", granted = roleName == "Admin" },
                        new { permissionName = "Manage Billing Settings", granted = roleName == "Admin" }
                    }
                }
            };

            // Get a sample of users to display
            var userSample = usersInRole.Take(5).Select(u => new
            {
                userName = $"{u.FirstName} {u.LastName}",
                profileImage = string.IsNullOrEmpty(u.ProfileImageUrl) ? "/images/default-avatar.png" : u.ProfileImageUrl
            }).ToList();

            var moreUsersCount = userCount > 5 ? userCount - 5 : 0;

            var data = new
            {
                roleName = roleName,
                isSystemRole = roleName == "Admin" || roleName == "Homeowner" || roleName == "Staff",
                roleDescription = GetRoleDescription(roleName),
                permissionCategories = permissionCategories,
                userCount = userCount,
                users = userSample,
                moreUsers = moreUsersCount > 0,
                moreUsersCount = moreUsersCount
            };

            return Json(new { success = true, data = data });
        }

        // Helper method to log user activity
        private async Task LogUserActivity(string userId, string activityType, string details)
        {
            // This would typically interact with your activity logging system
            // For now, we'll just return a task
            await Task.CompletedTask;
        }

        // Helper method to get role description
        private string GetRoleDescription(string roleName)
        {
            switch (roleName)
            {
                case "Admin":
                    return "Full access to all system features. Can manage users, settings, and all content.";
                case "Staff":
                    return "Access to service management and limited user information. Can process service requests and communicate with homeowners.";
                case "Homeowner":
                    return "Basic access to community features. Can view announcements, pay bills, and request services.";
                default:
                    return "Custom role with specific permissions.";
            }
        }
    }
}