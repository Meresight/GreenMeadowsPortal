using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Announcements")]
    public class AdminAnnouncementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AnnouncementService _announcementService;
        private readonly NotificationService _notificationService;

        public AdminAnnouncementController(
            UserManager<ApplicationUser> userManager,
            AnnouncementService announcementService,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _announcementService = announcementService;
            _notificationService = notificationService;
        }

        // GET: /Admin/Announcements
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string filter = "all", string search = "", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            // Create the view model
            var viewModel = new AnnouncementListViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                CurrentUserId = user.Id,
                CurrentUserRole = roles.FirstOrDefault() ?? "Admin",
                FilterCategory = filter,
                SearchQuery = search,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            // Get all announcements since we're in admin mode
            viewModel.Announcements = await _announcementService.GetAllAnnouncementsAsync(
                filter, search, page, 10, includeScheduled: true, includeDrafts: true);

            viewModel.TotalCount = await _announcementService.GetTotalCountAsync(filter, search);

            return View("~/Views/Announcement/Index.cshtml", viewModel);
        }

        // GET: /Admin/Announcements/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new AnnouncementCreateViewModel
            {
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "Admin",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            return View("~/Views/Announcement/Create.cshtml", viewModel);
        }

        // Other actions can be added to handle CRUD operations for announcements
        // For now, we'll redirect to the corresponding actions in the main AnnouncementController

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            return RedirectToAction("Details", "Announcement", new { id });
        }

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(int id)
        {
            return RedirectToAction("Edit", "Announcement", new { id });
        }

        [HttpGet("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            return RedirectToAction("Delete", "Announcement", new { id });
        }
    }
}