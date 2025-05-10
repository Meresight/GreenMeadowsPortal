using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenMeadowsPortal.Data; // Add this namespace for AppDbContext
namespace GreenMeadowsPortal.Controllers
{
    [Authorize] // Ensures only authenticated users can access
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AnnouncementService _announcementService;
        private readonly INotificationService _notificationService; // Changed to interface
        private readonly ServiceRequestService _serviceRequestService;
        private readonly UserService _userService;
        private readonly AppDbContext _context;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            AnnouncementService announcementService,
            INotificationService notificationService,
            ServiceRequestService serviceRequestService,
            UserService userService,
            AppDbContext context) // Changed to interface
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _announcementService = announcementService;
            _notificationService = notificationService;
            _serviceRequestService = serviceRequestService;
            _userService = userService;
            _context = context;
        }

        // Default action - redirect to appropriate dashboard based on role
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("AdminDashboard");
            else if (roles.Contains("Staff"))
                return RedirectToAction("StaffDashboard");
            else
                return RedirectToAction("HomeownerDashboard");
        }

        // ✅ Profile View for Dashboard
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            // Redirect to role-specific profile page
            if (roles.Contains("Admin"))
                return RedirectToAction("AdminProfile");
            else if (roles.Contains("Staff"))
                return RedirectToAction("StaffProfile");
            else
                return View("~/Views/Dashboard/Profile.cshtml", CreateProfileViewModel(user, roles));
        }

        // ✅ AdminProfile View
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var profileModel = CreateProfileViewModel(user, roles);

            // Add admin-specific details if needed

            return View(profileModel);
        }

        // ✅ StaffProfile View
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            // Create StaffProfileViewModel
            var staffProfileModel = new StaffProfileViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Staff",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                Department = user.Department ?? "Maintenance",
                EmployeeId = user.EmployeeId ?? "EMP" + new Random().Next(1000, 9999),
                Position = user.Position ?? "Staff Member",
                HireDate = user.MemberSince,
                Status = user.Status ?? "Active",
                // Set sample emergency contact data for demo purposes
                PrimaryContactName = "Jane Doe",
                PrimaryContactRelationship = "Spouse",
                PrimaryContactPhone = "(555) 123-4567",
                PrimaryContactEmail = "jane.doe@example.com",
                SecondaryContactName = "John Smith",
                SecondaryContactRelationship = "Parent",
                SecondaryContactPhone = "(555) 987-6543",
                SecondaryContactEmail = "john.smith@example.com",
                // Add sample skills for demo
                Skills = new List<StaffSkill>
                {
                    new StaffSkill
                    {
                        Id = 1,
                        Name = "Plumbing",
                        Type = "Technical",
                        Level = "Advanced",
                        AcquiredDate = new DateTime(2020, 5, 10),
                        ExpiryDate = null,
                        Status = "Active"
                    },
                    new StaffSkill
                    {
                        Id = 2,
                        Name = "Electrical Maintenance",
                        Type = "Technical",
                        Level = "Intermediate",
                        AcquiredDate = new DateTime(2021, 3, 15),
                        ExpiryDate = new DateTime(2025, 3, 15),
                        Status = "Active"
                    },
                    new StaffSkill
                    {
                        Id = 3,
                        Name = "CPR Certification",
                        Type = "Safety",
                        Level = "Certified",
                        AcquiredDate = new DateTime(2022, 1, 5),
                        ExpiryDate = new DateTime(2024, 1, 5),
                        Status = "Active"
                    }
                },
                // Sample work schedule
                Schedule = new WorkSchedule
                {
                    Monday = "8:00 AM - 4:00 PM",
                    Tuesday = "8:00 AM - 4:00 PM",
                    Wednesday = "8:00 AM - 4:00 PM",
                    Thursday = "8:00 AM - 4:00 PM",
                    Friday = "8:00 AM - 4:00 PM",
                    Saturday = "Off",
                    Sunday = "Off",
                    Notes = "Available for on-call emergencies on weekends."
                }
            };

            return View(staffProfileModel);
        }

        // ✅ Homeowner Dashboard
        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> HomeownerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var roles = await _userManager.GetRolesAsync(user);
            var activeRequests = await _serviceRequestService.GetUserServiceRequestsAsync(user.Id, "open");
            var allRequests = await _serviceRequestService.GetUserServiceRequestsAsync(user.Id, "all");
            var model = new HomeownerDashboardViewModel
            {
                HomeownerUser = user,
                FirstName = user.FirstName ?? "Homeowner",
                Role = roles.FirstOrDefault() ?? "Homeowner",
                TotalPropertiesOwned = 2,
                PendingRequests = activeRequests.Count,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = user != null ? await _notificationService.GetUnreadCountAsync(user.Id) : 0,
                RecentAnnouncements = await _announcementService.GetRecentAnnouncementsAsync(3),
                // Add more fields as needed
            };
            // Optionally, add recent requests to the model if you want to show them
            // model.RecentRequests = allRequests.Take(5).ToList();
            return View(model);
        }

        // ✅ Admin Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var roles = await _userManager.GetRolesAsync(user);
            var totalUsers = await _userManager.Users.CountAsync();
            var pendingRequests = (await _serviceRequestService.GetAllServiceRequestsAsync("open")).Count;
            var recentRequests = await _serviceRequestService.GetAllServiceRequestsAsync("all");

            // Get feedback statistics
            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .OrderByDescending(f => f.SubmittedDate)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                AdminUser = user,
                FirstName = user.FirstName ?? "Admin",
                Role = roles.FirstOrDefault() ?? "Admin",
                TotalUsers = totalUsers,
                ActiveReservations = 20,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                PendingRequests = pendingRequests,
                OutstandingDues = 24500.00M,
                UpcomingEvents = 3,
                NotificationCount = user != null ? await _notificationService.GetUnreadCountAsync(user.Id) : 0,
                // Feedback statistics
                TotalFeedbacks = await _context.Feedbacks.CountAsync(),
                NewFeedbacks = await _context.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.New),
                ResolvedFeedbacks = await _context.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.Resolved),
                RecentFeedbacks = feedbacks.Select(f => new FeedbackItemViewModel
                {
                    Id = f.Id,
                    Type = f.Type,
                    Subject = f.Subject,
                    Status = f.Status,
                    SubmittedDate = f.SubmittedDate,
                    SubmittedBy = $"{f.User.FirstName} {f.User.LastName}",
                    SubmittedByAvatar = f.User.ProfileImageUrl ?? "/images/default-avatar.png",
                    Priority = f.Priority,
                    Category = f.Category
                }).ToList(),
                RecentActivities = recentRequests.Take(5).Select(r => new ActivityViewModel {
                    ActivityType = r.IssueType,
                    Description = r.Description,
                    Timestamp = r.DateSubmitted,
                    UserName = r.Requester?.FirstName + " " + r.Requester?.LastName,
                    IconClass = "fa-tools",
                    ReferenceId = r.Id,
                    ActionUrl = $"/ServiceRequests/Details/{r.Id}"
                }).ToList()
            };
            return View(model);
        }

        // ✅ Staff Dashboard
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");
            var roles = await _userManager.GetRolesAsync(user);
            var totalResidents = await _userManager.Users.CountAsync();
            var pendingRequests = (await _serviceRequestService.GetAllServiceRequestsAsync("open")).Count;
            var recentRequests = await _serviceRequestService.GetAllServiceRequestsAsync("all");

            // Get feedback statistics
            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .OrderByDescending(f => f.SubmittedDate)
                .Take(5)
                .ToListAsync();

            var model = new StaffDashboardViewModel
            {
                StaffUser = user,
                FirstName = user.FirstName ?? "Staff",
                Role = roles.FirstOrDefault() ?? "Staff",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = user != null ? await _notificationService.GetUnreadCountAsync(user.Id) : 0,
                TotalResidents = totalResidents,
                PendingRequests = pendingRequests,
                // Feedback statistics
                TotalFeedbacks = await _context.Feedbacks.CountAsync(),
                NewFeedbacks = await _context.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.New),
                ResolvedFeedbacks = await _context.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.Resolved),
                RecentFeedbacks = feedbacks.Select(f => new FeedbackItemViewModel
                {
                    Id = f.Id,
                    Type = f.Type,
                    Subject = f.Subject,
                    Status = f.Status,
                    SubmittedDate = f.SubmittedDate,
                    SubmittedBy = $"{f.User.FirstName} {f.User.LastName}",
                    SubmittedByAvatar = f.User.ProfileImageUrl ?? "/images/default-avatar.png",
                    Priority = f.Priority,
                    Category = f.Category
                }).ToList(),
                RecentServiceRequests = recentRequests.Take(5).Select(r => new DashboardServiceRequest {
                    Id = r.Id,
                    Title = r.IssueType,
                    RequesterName = r.Requester?.FirstName + " " + r.Requester?.LastName,
                    Category = r.IssueType,
                    Status = r.Status.ToString(),
                    StatusClass = r.Status == ServiceRequestStatus.Open ? "status-open" : "status-closed",
                    DateSubmitted = r.DateSubmitted
                }).ToList(),
                RecentUsers = recentRequests.Take(5).Select(r => new GreenMeadowsPortal.ViewModels.DashboardUser {
                    Id = r.Requester?.Id ?? string.Empty,
                    Name = r.Requester != null ? $"{r.Requester.FirstName} {r.Requester.LastName}" : "Unknown",
                    FullName = r.Requester != null ? $"{r.Requester.FirstName} {r.Requester.LastName}" : "Unknown",
                    Email = r.Requester?.Email ?? string.Empty,
                    Role = "User",
                    JoinDate = r.Requester?.MemberSince ?? DateTime.MinValue,
                    ProfileImageUrl = r.Requester?.ProfileImageUrl ?? "/images/default-avatar.png",
                    ActivityDescription = r.Requester != null ? $"Joined on {r.Requester.MemberSince:MMM dd, yyyy}" : "Unknown",
                    ActivityTime = r.Requester != null ? r.Requester.MemberSince.ToString("MMM dd, yyyy") : "Unknown"
                }).ToList()
            };
            return View(model);
        }

        // Redirects to the Announcement controller's Index action instead of having a duplicate view
        public IActionResult Announcement()
        {
            return RedirectToAction("Index", "Announcement");
        }

        // ✅ Billing
        [Authorize]
        public async Task<IActionResult> Billing(int? year)
        {
            // Redirect to the role-specific billing page instead of showing content directly
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminBilling", new { year });
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("StaffBilling", new { year });
            }
            else
            {
                return RedirectToAction("HomeownerBilling", new { year });
            }
        }

        // Helper method to create a ProfileViewModel
        private ProfileViewModel CreateProfileViewModel(ApplicationUser user, IList<string> roles)
        {
            return new ProfileViewModel
            {
                FullName = $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim(),
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? "No address available",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                MemberSince = user.MemberSince != default ? user.MemberSince.ToString("MMMM yyyy") : "Unknown",
                Status = user.Status ?? "Unknown",
                Role = roles.FirstOrDefault() ?? "User",
                NotificationCount = _notificationService.GetUnreadCountAsync(user.Id).Result
            };
        }

        // Helper methods to populate role-specific billing data
        private Task PopulateAdminBillingData(BillingViewModel model, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        private Task PopulateHomeownerBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }

            if (model.CurrentBilling?.DueDate != null && model.CurrentBilling.DueDate != default)
            {
                var daysUntil = (model.CurrentBilling.DueDate - DateTime.Now).Days;
                model.DaysUntilDue = daysUntil > 0 ? daysUntil : 0;
            }
            else
            {
                model.DaysUntilDue = 0;
            }

            return Task.CompletedTask;
        }

        private Task PopulateStaffBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        private Task PopulateDefaultBillingData(BillingViewModel model, string userId, int selectedYear)
        {
            var currentYear = DateTime.Now.Year;
            for (int i = currentYear; i >= currentYear - 3; i--)
            {
                model.YearOptions.Add(new SelectListItem
                {
                    Value = i.ToString(),
                    Text = i.ToString(),
                    Selected = i == selectedYear
                });
            }
            return Task.CompletedTask;
        }

        // Add this to your DashboardController.cs
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound("User not found");

                // Fix for line 387 - null check before GetRolesAsync
                var roles = user != null
                    ? await _userManager.GetRolesAsync(user)
                    : new List<string>();

                // Create a simplified model - we'll bypass complex models for now
                var viewModel = new UserManagementViewModel
                {
                    FirstName = user?.FirstName ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Admin",
                    NotificationCount = user != null ? await _notificationService.GetUnreadCountAsync(user.Id) : 0,
                    CurrentUserProfileImageUrl = user?.ProfileImageUrl ?? "/images/default-avatar.png",
                    Users = new List<UserViewModel>(), // Initialize required property
                    PendingUsers = new List<PendingUserViewModel>(), // Initialize required property
                    Roles = new List<RoleViewModel>(), // Initialize required property
                    ActivityLogs = new List<ActivityLogViewModel>(), // Initialize required property
                    CurrentPage = 1,
                    TotalPages = 1,
                    TotalRecords = 0,
                    PageStartRecord = 1,
                    PageEndRecord = 0,
                    PendingRequestsCount = 0,
                    LogsCurrentPage = 1,
                    LogsTotalPages = 1,
                    LogsPageStartRecord = 1,
                    LogsPageEndRecord = 0,
                    TotalLogs = 0
                };

                // DIRECTLY fetch users without using the service
                var allUsers = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserViewModel>();

                foreach (var u in allUsers)
                {
                    // Skip null users
                    if (u == null) continue;

                    var userRoles = await _userManager.GetRolesAsync(u);
                    userViewModels.Add(new UserViewModel
                    {
                        Id = u.Id,
                        FirstName = u.FirstName ?? "",
                        LastName = u.LastName ?? "",
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber ?? "",
                        Address = u.Address ?? "",
                        Role = userRoles.FirstOrDefault() ?? "User",
                        Status = u.Status ?? "Active",
                        MemberSince = u.MemberSince != default ? u.MemberSince.ToString("MMM dd, yyyy") : "Unknown",
                        LastLogin = u.LastLoginDate.HasValue ?
                            u.LastLoginDate.Value.ToString("MMM dd, yyyy HH:mm") : "Never",
                        ProfileImageUrl = u.ProfileImageUrl ?? "/images/default-avatar.png",
                        PropertyType = u.PropertyType ?? "Unknown", // Added to fix CS9035
                        OwnershipStatus = u.OwnershipStatus ?? "Unknown" // Added to fix CS
                    });
                }

                viewModel.Users = userViewModels;

                // Simplified for now
                viewModel.CurrentPage = 1;
                viewModel.TotalPages = 1;
                viewModel.TotalRecords = userViewModels.Count;
                viewModel.PageStartRecord = 1;
                viewModel.PageEndRecord = userViewModels.Count;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Return a simple model with error information
                return View(new UserManagementViewModel
                {
                    FirstName = "Error occurred: " + ex.Message,
                    Role = "Admin",
                    Users = new List<UserViewModel>(),
                    PendingUsers = new List<PendingUserViewModel>(), // Initialize required property
                    Roles = new List<RoleViewModel>(), // Initialize required property
                    ActivityLogs = new List<ActivityLogViewModel>(), // Initialize required property
                    CurrentPage = 1,
                    TotalPages = 1,
                    TotalRecords = 0,
                    PageStartRecord = 1,
                    PageEndRecord = 0,
                    PendingRequestsCount = 0,
                    LogsCurrentPage = 1,
                    LogsTotalPages = 1,
                    LogsPageStartRecord = 1,
                    LogsPageEndRecord = 0,
                    TotalLogs = 0
                });
            }
        }
        // Add this to your DashboardController.cs

        // This method will handle redirecting users to the appropriate billing page based on role
        [Authorize]
        public async Task<IActionResult> BillingRedirect()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminBilling");
            }
            else if (roles.Contains("Staff"))
            {
                return RedirectToAction("StaffBilling");
            }
            else
            {
                return RedirectToAction("HomeownerBilling");
            }
        }
        // Add these methods to your DashboardController.cs

        // Method to prepare Admin billing view model
        private async Task<BillingViewModel> PrepareAdminBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Admin",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            // Fix for line 411 - null check before GetRolesAsync is now handled with the early return above
            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Admin",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate admin-specific billing data
            await PopulateAdminBillingData(model, selectedYear);

            return model;
        }

        // Method to prepare Staff billing view model
        private async Task<BillingViewModel> PrepareStaffBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Staff",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            // Fix for line 435 - null check before GetRolesAsync is now handled with the early return above
            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Staff",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate staff-specific billing data
            await PopulateStaffBillingData(model, user.Id, selectedYear);

            return model;
        }

        // Method to prepare Homeowner billing view model
        private async Task<BillingViewModel> PrepareHomeownerBillingViewModel(int? year)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Return a minimal model if user is null
                return new BillingViewModel
                {
                    FirstName = "",
                    LastName = "",
                    Role = "Homeowner",
                    ProfileImageUrl = "/images/default-avatar.png",
                    NotificationCount = 0,
                    SelectedYear = year ?? DateTime.Now.Year,
                    YearOptions = new List<SelectListItem>()
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentYear = DateTime.Now.Year;
            var selectedYear = year ?? currentYear;

            var model = new BillingViewModel
            {
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Role = roles.FirstOrDefault() ?? "Homeowner",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                SelectedYear = selectedYear
            };

            // Populate homeowner-specific billing data
            await PopulateHomeownerBillingData(model, user.Id, selectedYear);

            return model;
        }
        // Create specific billing actions for each role
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminBilling(int? year)
        {
            // Admin-specific billing view
            var model = await PrepareAdminBillingViewModel(year);
            return View("AdminBilling", model);
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffBilling(int? year)
        {
            // Staff-specific billing view
            var model = await PrepareStaffBillingViewModel(year);
            return View("StaffBilling", model);
        }

        [Authorize(Roles = "Homeowner")]
        public async Task<IActionResult> HomeownerBilling(int? year)
        {
            // Homeowner-specific billing view
            var model = await PrepareHomeownerBillingViewModel(year);
            return View("Billing", model);
        }

        // ✅ Logout Method
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}