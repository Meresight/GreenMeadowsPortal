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

namespace GreenMeadowsPortal.Controllers.ApiControllers
{
    [Route("api/announcements")]
    [ApiController]
    [Authorize]
    public class AnnouncementApiController : ControllerBase
    {
        private readonly AnnouncementService _announcementService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public AnnouncementApiController(
            AnnouncementService announcementService,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _announcementService = announcementService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: api/announcements
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements(string filter = "all", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");
            var isStaff = roles.Contains("Staff");

            List<AnnouncementDetailsViewModel> announcements;

            if (isAdmin)
            {
                announcements = await _announcementService.GetAllAnnouncementsAsync(
                    filter, "", page, 10, includeScheduled: true, includeDrafts: true);
            }
            else if (isStaff)
            {
                announcements = await _announcementService.GetAnnouncementsForStaffAsync(
                    user.Id, filter, "", page, 10);
            }
            else
            {
                announcements = await _announcementService.GetAnnouncementsForHomeownersAsync(
                    filter, "", page, 10);
            }

            // Mark which announcements the user has read
            foreach (var announcement in announcements)
            {
                announcement.HasBeenRead = await _announcementService.HasUserReadAnnouncementAsync(announcement.Id, user.Id);
            }

            var totalCount = await _announcementService.GetTotalCountAsync(filter);
            var totalPages = (int)Math.Ceiling(totalCount / 10.0);

            return Ok(new
            {
                announcements,
                currentPage = page,
                totalPages,
                totalCount
            });
        }

        // GET: api/announcements/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnnouncement(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
                return NotFound();

            // Check if the user has read this announcement
            announcement.HasBeenRead = await _announcementService.HasUserReadAnnouncementAsync(id, user.Id);

            return Ok(announcement);
        }

        // GET: api/announcements/recent
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentAnnouncements(int count = 3)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var announcements = await _announcementService.GetRecentAnnouncementsAsync(count);

            // Mark which announcements the user has read
            foreach (var announcement in announcements)
            {
                announcement.HasBeenRead = await _announcementService.HasUserReadAnnouncementAsync(announcement.Id, user.Id);
            }

            return Ok(announcements);
        }

        // POST: api/announcements/{id}/read
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
                return NotFound();

            await _announcementService.MarkAsReadAsync(id, user.Id);
            return Ok();
        }

        // GET: api/announcements/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var count = await _announcementService.GetUnreadAnnouncementsCountAsync(user.Id);
            return Ok(new { count });
        }
    }
}