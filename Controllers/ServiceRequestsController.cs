using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly ServiceRequestService _serviceRequestService;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            ServiceRequestService serviceRequestService,
            ILogger<ServiceRequestsController> logger)
        {
            _userManager = userManager;
            _notificationService = notificationService;
            _serviceRequestService = serviceRequestService;
            _logger = logger;
        }

        // GET: /ServiceRequest
        public async Task<IActionResult> Index(string statusFilter = "all")
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login", "Account");

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                // Create the view model
                var viewModel = new ServiceRequestListViewModel
                {
                    FirstName = user.FirstName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Homeowner",
                    ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                    NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                    StatusFilter = statusFilter
                };

                // Get the appropriate service requests based on role
                if (isAdmin || isStaff)
                {
                    viewModel.Requests = await _serviceRequestService.GetAllServiceRequestsAsync(statusFilter);
                    viewModel.TotalOpenRequests = await _serviceRequestService.GetTotalOpenRequestsCountAsync();
                    viewModel.TotalClosedRequests = await _serviceRequestService.GetTotalClosedRequestsCountAsync();
                }
                else
                {
                    viewModel.Requests = await _serviceRequestService.GetUserServiceRequestsAsync(user.Id, statusFilter);
                    viewModel.TotalOpenRequests = await _serviceRequestService.GetUserOpenRequestsCountAsync(user.Id);
                    viewModel.TotalClosedRequests = await _serviceRequestService.GetUserClosedRequestsCountAsync(user.Id);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ServiceRequest Index action");
                TempData["ErrorMessage"] = "An error occurred while loading service requests.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: /ServiceRequest/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            
            // Check if user has any role
            if (!roles.Any())
            {
                // If no role is assigned, assign Homeowner role
                await _userManager.AddToRoleAsync(user, "Homeowner");
                roles = await _userManager.GetRolesAsync(user);
            }

            var model = new ServiceRequestCreateViewModel
            {
                FirstName = user.FirstName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Homeowner",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                Location = user.Unit ?? string.Empty // Pre-populate with user's unit
            };

            return View(model);
        }

        // POST: /ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequestCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("Attempted to create service request with no logged-in user.");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("Service request creation attempt by user: {UserId}, email: {Email}", user.Id, user.Email);

            var roles = await _userManager.GetRolesAsync(user);
            
            // Check if user has any role
            if (!roles.Any())
            {
                // If no role is assigned, assign Homeowner role
                await _userManager.AddToRoleAsync(user, "Homeowner");
                roles = await _userManager.GetRolesAsync(user);
            }

            if (ModelState.IsValid)
            {
                // Only use the existing user, never create a new one
                var success = await _serviceRequestService.CreateServiceRequestAsync(user.Id, model);
                if (success)
                {
                    // Notify all staff and admins
                    var allUsers = _userManager.Users.ToList();
                    foreach (var u in allUsers)
                    {
                        var userRoles = await _userManager.GetRolesAsync(u);
                        if (userRoles.Contains("Admin") || userRoles.Contains("Staff"))
                        {
                            await _notificationService.CreateNotificationAsync(
                                u.Id,
                                "New Service Request",
                                $"A new service request has been submitted by {user.FullName}.",
                                "ServiceRequest"
                            );
                        }
                    }
                    TempData["SuccessMessage"] = "Service request submitted successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to submit service request. Please try again.";
                }
            }

            // If we got here, something went wrong. Redisplay form
            model.FirstName = user.FirstName ?? string.Empty;
            model.Role = roles.FirstOrDefault() ?? "Homeowner";
            model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
            model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return View(model);
        }

        // GET: /ServiceRequest/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var serviceRequest = await _serviceRequestService.GetServiceRequestByIdAsync(id);
            if (serviceRequest == null)
                return NotFound();

            // Check if user is authorized to view this request
            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");
            var isStaff = roles.Contains("Staff");

            // Only allow if user is admin/staff or if they submitted the request
            if (!isAdmin && !isStaff && serviceRequest.RequesterId != user.Id)
            {
                return Forbid();
            }

            // Get available staff for assignment
            var availableStaff = await _serviceRequestService.GetAvailableStaffAsync();
            ViewBag.AvailableStaff = availableStaff;

            // Set permissions in ViewBag
            ViewBag.Role = roles.FirstOrDefault() ?? "Homeowner";
            ViewBag.CanClose = (isAdmin || isStaff) && serviceRequest.Status == ServiceRequestStatus.Open;
            ViewBag.CanReopen = (isAdmin || isStaff) && serviceRequest.Status == ServiceRequestStatus.Closed;
            ViewBag.CanEdit = (isAdmin || isStaff) && serviceRequest.Status == ServiceRequestStatus.Open;
            ViewBag.CanAssignStaff = isAdmin && serviceRequest.Status == ServiceRequestStatus.Open;

            return View(serviceRequest);
        }

        // POST: /ServiceRequest/Close/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Close(int id, string? adminNotes = null)
        {
            try
            {
                var success = await _serviceRequestService.CloseServiceRequestAsync(id, adminNotes);
                if (success)
                {
                    // Notify the requester
                    var request = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                    if (request != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            request.RequesterId,
                            "Service Request Closed",
                            $"Your service request (ID: {id}) has been closed.",
                            "ServiceRequest",
                            id.ToString()
                        );
                    }
                    TempData["SuccessMessage"] = "Service request closed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to close service request.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Close action");
                TempData["ErrorMessage"] = "An error occurred while closing the service request.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: /ServiceRequest/Reopen/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Reopen(int id)
        {
            try
            {
                var success = await _serviceRequestService.ReopenServiceRequestAsync(id);
                if (success)
                {
                    // Notify the requester
                    var request = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                    if (request != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            request.RequesterId,
                            "Service Request Reopened",
                            $"Your service request (ID: {id}) has been reopened.",
                            "ServiceRequest",
                            id.ToString()
                        );
                    }
                    TempData["SuccessMessage"] = "Service request reopened successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reopen service request.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Reopen action");
                TempData["ErrorMessage"] = "An error occurred while reopening the service request.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: /ServiceRequest/AssignStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AssignStaff(int id, string staffId)
        {
            try
            {
                var success = await _serviceRequestService.AssignStaffToRequestAsync(id, staffId);
                if (success)
                {
                    // Notify the requester
                    var request = await _serviceRequestService.GetServiceRequestByIdAsync(id);
                    if (request != null)
                    {
                        await _notificationService.CreateNotificationAsync(
                            request.RequesterId,
                            "Staff Assigned to Service Request",
                            $"A staff member has been assigned to your service request (ID: {id}).",
                            "ServiceRequest",
                            id.ToString()
                        );
                    }
                    TempData["SuccessMessage"] = "Staff assigned successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to assign staff.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignStaff action");
                TempData["ErrorMessage"] = "An error occurred while assigning staff.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}