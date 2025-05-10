using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;


namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public NotificationController(
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService)
        {
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: /Notification/
        public async Task<IActionResult> Index(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var notifications = await _notificationService.GetNotificationsForUserAsync(user.Id, page, 10);
            var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);

            var viewModel = new NotificationsListViewModel
            {
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "User",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Notifications = notifications,
                UnreadCount = unreadCount,
                TotalCount = notifications.Count, // This should be replaced with a total count query
                CurrentPage = page,
                TotalPages = (int)System.Math.Ceiling(notifications.Count / 10.0) // This should use the total count
            };

            return View(viewModel);
        }

        // POST: /Notification/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteNotificationAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            await _notificationService.DeleteAllNotificationsAsync(user.Id);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Notification/Preferences
        public async Task<IActionResult> Preferences()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new NotificationPreferencesViewModel
            {
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "User",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                ReceiveEmailNotifications = user.ReceiveEmailNotifications ?? true,
                ReceiveSmsNotifications = user.ReceiveSmsNotifications ?? false
                // Other preferences could be loaded from a separate preferences table
            };

            return View(viewModel);
        }

        // POST: /Notification/SavePreferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePreferences(NotificationPreferencesViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Update user preferences
            user.ReceiveEmailNotifications = model.ReceiveEmailNotifications;
            user.ReceiveSmsNotifications = model.ReceiveSmsNotifications;

            // Save changes
            await _userManager.UpdateAsync(user);

            // Additional preferences could be saved to a separate preferences table

            TempData["SuccessMessage"] = "Notification preferences updated successfully.";
            return RedirectToAction(nameof(Preferences));
        }

        // AJAX endpoints for notification updates

        // GET: /Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(user.Id);
            return Json(new { count });
        }

        // GET: /Notification/GetLatestNotifications
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new
                {
                    notifications = Array.Empty<object>()
                });
        
        var notifications = await _notificationService.GetNotificationsForUserAsync(user.Id, 1, 5);
            return Json(new { notifications });
        }
    }
}