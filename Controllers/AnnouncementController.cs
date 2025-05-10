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
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly AnnouncementService _announcementService;
        private readonly NotificationService _notificationService;
        private readonly ILogger<AnnouncementController> _logger;



        public AnnouncementController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment hostEnvironment,
            AnnouncementService announcementService,
            NotificationService notificationService,
                ILogger<AnnouncementController> logger)

        {
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _announcementService = announcementService;
            _notificationService = notificationService;
            _logger = logger;

        }

        // GET: /Announcement/
        // Shows list of announcements based on user role
        public async Task<IActionResult> Index(string filter = "all", string search = "", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");
            var isStaff = roles.Contains("Staff");

            // Get appropriate announcements based on role
            var viewModel = new AnnouncementListViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                CurrentUserId = user.Id,
                CurrentUserRole = roles.FirstOrDefault() ?? "User",
                FilterCategory = filter,
                SearchQuery = search,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            // Get announcements based on role
            if (isAdmin)
            {
                // Admins see all announcements, including drafts and scheduled
                viewModel.Announcements = await _announcementService.GetAllAnnouncementsAsync(
                    filter, search, page, 10, includeScheduled: true, includeDrafts: true);
            }
            else if (isStaff)
            {
                // Staff see published announcements targeted at staff or all, plus their own drafts
                viewModel.Announcements = await _announcementService.GetAnnouncementsForStaffAsync(
                    user.Id, filter, search, page, 10);
            }
            else
            {
                // Homeowners see only published announcements targeted at homeowners or all
                viewModel.Announcements = await _announcementService.GetAnnouncementsForHomeownersAsync(
                    filter, search, page, 10);
            }

            viewModel.TotalCount = await _announcementService.GetTotalCountAsync(filter, search);
            return View(viewModel);
        }



        // GET: /Announcement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
                return NotFound();

            // Mark as read for this user
            await _announcementService.MarkAsReadAsync(id, user.Id);

            return View(announcement);
        }

        // GET: /Announcement/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new AnnouncementCreateViewModel
            {
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "User",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png"
            };

            return View(viewModel);
        }

        // POST: /Announcement/Create
        // POST: /Announcement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // If staff, check if the announcement requires approval based on priority
            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");
            if (!isAdmin && model.Priority == AnnouncementPriority.Urgent)
            {
                // Staff can't directly publish urgent announcements
                ModelState.AddModelError("Priority", "Staff members cannot create urgent announcements without admin approval.");
            }

            if (ModelState.IsValid)
            {
                string? attachmentUrl = null;
                string? imageUrl = null;

                // Process attachment if uploaded
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    attachmentUrl = await SaveFileAsync(model.Attachment, "attachments");
                }

                // Process image if uploaded
                if (model.Image != null && model.Image.Length > 0)
                {
                    // Log information about the image
                    _logger.LogInformation($"Image upload: Name={model.Image.FileName}, Size={model.Image.Length}, ContentType={model.Image.ContentType}");

                    try
                    {
                        imageUrl = await SaveFileAsync(model.Image, "images/announcements");
                        _logger.LogInformation($"Image saved successfully at: {imageUrl}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error saving image: {ex.Message}");
                        ModelState.AddModelError("Image", $"Error saving image: {ex.Message}");
                    }
                }

                var announcement = new AdminAnnouncement
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedDate = DateTime.Now,
                    PublishDate = model.PublishDate ?? DateTime.Now,
                    ExpirationDate = model.ExpirationDate,
                    AuthorId = user.Id,
                    Priority = model.Priority,
                    Status = isAdmin ?
                        (model.SaveAsDraft ? AnnouncementStatus.Draft : AnnouncementStatus.Published) :
                        (model.Priority == AnnouncementPriority.Urgent ? AnnouncementStatus.Draft : AnnouncementStatus.Published),
                    TargetAudience = model.TargetAudience ?? "All",
                    AttachmentUrl = attachmentUrl ?? string.Empty, // Ensure non-null value
                    ImageUrl = imageUrl ?? string.Empty           // Ensure non-null value
                };

                await _announcementService.CreateAnnouncementAsync(announcement);

                // Send notifications if published immediately
                if (announcement.Status == AnnouncementStatus.Published && announcement.PublishDate <= DateTime.Now)
                {
                    await SendAnnouncementNotificationsAsync(announcement);
                }

                if (announcement.Status == AnnouncementStatus.Draft)
                {
                    TempData["SuccessMessage"] = "Announcement saved as draft.";
                }
                else if (announcement.PublishDate > DateTime.Now)
                {
                    TempData["SuccessMessage"] = "Announcement scheduled for publication.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Announcement published successfully.";
                }

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Announcement/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            // Admins can edit any announcement
            if (!isAdmin && announcement.AuthorId != user.Id)
            {
                return Forbid();
            }

            var viewModel = new AnnouncementEditViewModel
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                PublishDate = announcement.PublishDate,
                ExpirationDate = announcement.ExpirationDate,
                Priority = announcement.Priority,
                Status = announcement.Status,  // Keep the current status
                TargetAudience = announcement.TargetAudience,
                ExistingAttachmentUrl = announcement.AttachmentUrl,
                ExistingImageUrl = announcement.ImageUrl,
                AuthorName = $"{announcement.AuthorName}",
                CreatedDate = announcement.CreatedDate,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                FirstName = user.FirstName,
                Role = roles.FirstOrDefault() ?? "User"
            };

            // Add user role to ViewBag for breadcrumbs
            ViewBag.UserRole = roles.FirstOrDefault() ?? "Admin";

            return View(viewModel);
        }

        // POST: /Announcement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, AnnouncementEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                string attachmentUrl = announcement.AttachmentUrl;
                string imageUrl = announcement.ImageUrl;

                // Process new attachment if uploaded
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    // Delete old attachment if exists
                    if (!string.IsNullOrEmpty(announcement.AttachmentUrl))
                    {
                        DeleteFile(announcement.AttachmentUrl);
                    }
                    attachmentUrl = await SaveFileAsync(model.Attachment, "attachments");
                }

                // Process new image if uploaded
                if (model.Image != null && model.Image.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(announcement.ImageUrl))
                    {
                        DeleteFile(announcement.ImageUrl);
                    }
                    imageUrl = await SaveFileAsync(model.Image, "images/announcements");
                }

                // Ensure Status can be changed from Draft to Published
                // Always respect the status selected in the form for admin users

                // Update announcement properties
                announcement.Title = model.Title;
                announcement.Content = model.Content;
                announcement.PublishDate = model.PublishDate ?? DateTime.Now;
                announcement.ExpirationDate = model.ExpirationDate;
                announcement.Priority = model.Priority;
                announcement.Status = model.Status; // Allow status change (Draft to Published or vice versa)
                announcement.TargetAudience = model.TargetAudience;
                announcement.AttachmentUrl = attachmentUrl;
                announcement.ImageUrl = imageUrl;

                var adminAnnouncement = new AdminAnnouncement
                {
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Content = announcement.Content,
                    CreatedDate = announcement.CreatedDate,
                    PublishDate = announcement.PublishDate,
                    ExpirationDate = announcement.ExpirationDate,
                    AuthorId = announcement.AuthorId,
                    Priority = announcement.Priority,
                    Status = announcement.Status,
                    TargetAudience = announcement.TargetAudience,
                    AttachmentUrl = announcement.AttachmentUrl,
                    ImageUrl = announcement.ImageUrl
                };

                await _announcementService.UpdateAnnouncementAsync(adminAnnouncement);

                // Send notifications if published now
                if (announcement.Status == AnnouncementStatus.Published &&
                    announcement.PublishDate <= DateTime.Now)
                {
                    await SendAnnouncementNotificationsAsync(adminAnnouncement);
                }

                TempData["SuccessMessage"] = "Announcement updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: /Announcement/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: /Announcement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            // Delete attachment and image files if they exist
            if (!string.IsNullOrEmpty(announcement.AttachmentUrl))
            {
                DeleteFile(announcement.AttachmentUrl);
            }

            if (!string.IsNullOrEmpty(announcement.ImageUrl))
            {
                DeleteFile(announcement.ImageUrl);
            }

            await _announcementService.DeleteAnnouncementAsync(id);
            TempData["SuccessMessage"] = "Announcement deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Announcement/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            await _announcementService.MarkAsReadAsync(id, user.Id);
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: /Announcement/ReadReceipts/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReadReceipts(int id)
        {
            var announcement = await _announcementService.GetAnnouncementWithReadReceiptsAsync(id);
            if (announcement == null)
            {
                return NotFound();
            }

            return View(announcement);
        }

        // POST: /Announcement/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            try
            {
                _logger.LogInformation($"Publish method called for announcement ID: {id}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Publish failed: User not authenticated");
                    return RedirectToAction("Login", "Account");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");
                _logger.LogInformation($"User roles: Admin={isAdmin}, Staff={isStaff}");

                var announcement = await _announcementService.GetAnnouncementByIdAsync(id);
                if (announcement == null)
                {
                    _logger.LogWarning($"Publish failed: Announcement with ID {id} not found");
                    return NotFound();
                }
                _logger.LogInformation($"Found announcement: ID={id}, Title={announcement.Title}, CurrentStatus={announcement.Status}");

                // Check if user has permission to publish
                if (!isAdmin && !(isStaff && announcement.AuthorId == user.Id))
                {
                    _logger.LogWarning($"Publish failed: User {user.Id} does not have permission to publish announcement {id}");
                    return Forbid();
                }

                // Update announcement status to Published
                announcement.Status = AnnouncementStatus.Published;

                // If publish date is in the future, set it to now
                if (announcement.PublishDate > DateTime.Now)
                {
                    _logger.LogInformation($"Setting future publish date to current time. Old: {announcement.PublishDate}, New: {DateTime.Now}");
                    announcement.PublishDate = DateTime.Now;
                }

                var adminAnnouncement = new AdminAnnouncement
                {
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Content = announcement.Content,
                    CreatedDate = announcement.CreatedDate,
                    PublishDate = announcement.PublishDate,
                    ExpirationDate = announcement.ExpirationDate,
                    AuthorId = announcement.AuthorId,
                    Priority = announcement.Priority,
                    Status = announcement.Status,
                    TargetAudience = announcement.TargetAudience,
                    AttachmentUrl = announcement.AttachmentUrl,
                    ImageUrl = announcement.ImageUrl
                };

                try
                {
                    await _announcementService.UpdateAnnouncementAsync(adminAnnouncement);
                    _logger.LogInformation($"Successfully updated announcement {id} status to Published");

                    // Send notifications
                    await SendAnnouncementNotificationsAsync(adminAnnouncement);

                    TempData["SuccessMessage"] = "Announcement published successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error publishing announcement {id}: {ex.Message}");
                    TempData["ErrorMessage"] = "Failed to publish announcement: " + ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in Publish method: {ex.Message}");
                TempData["ErrorMessage"] = "An unexpected error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }
        // Private methods
        // Updated SaveFileAsync method with better error handling
        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            try
            {
                // Validate file is not null
                if (file == null || file.Length == 0)
                    return string.Empty;

                // Get and create uploads folder if it doesn't exist
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, subfolder);

                if (!Directory.Exists(uploadsFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error creating directory: {ex.Message}");
                        // Fall back to a default directory that should exist
                        uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);
                    }
                }

                // Clean the filename to prevent any path traversal attacks
                string fileName = Path.GetFileName(file.FileName);

                // Generate unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/{subfolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving file: {ex.Message}");
                return string.Empty;
            }
        }
        private void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            try
            {
                // Remove leading slash
                string relativePath = fileUrl.TrimStart('/');
                string fullPath = Path.Combine(_hostEnvironment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        private async Task SendAnnouncementNotificationsAsync(AdminAnnouncement announcement)
        {
            // Get users to notify based on target audience
            IEnumerable<ApplicationUser> usersToNotify;

            if (announcement.TargetAudience == "All")
            {
                usersToNotify = await _userManager.Users.ToListAsync();
            }
            else
            {
                // Get users in specific role(s)
                var userIds = new List<string>();

                if (announcement.TargetAudience == "Homeowners" || announcement.TargetAudience.Contains("Homeowners"))
                {
                    var homeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
                    userIds.AddRange(homeowners.Select(u => u.Id));
                }
                else if (announcement.TargetAudience == "Staff" || announcement.TargetAudience.Contains("Staff"))
                {
                    var staff = await _userManager.GetUsersInRoleAsync("Staff");
                    userIds.AddRange(staff.Select(u => u.Id));
                }
                else if (announcement.TargetAudience == "Administrators" || announcement.TargetAudience.Contains("Administrators"))
                {
                    var admins = await _userManager.GetUsersInRoleAsync("Admin");
                    userIds.AddRange(admins.Select(u => u.Id));
                }

                usersToNotify = await _userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();
            }

            // Create notification for each user
            foreach (var user in usersToNotify)
            {
                // Skip notifications for users who already read this (for updates)
                var hasRead = await _announcementService.HasUserReadAnnouncementAsync(announcement.Id, user.Id);
                if (hasRead) continue;

                // Create notification
                await _notificationService.CreateNotificationAsync(
                    userId: user.Id,
                    title: "New Announcement",
                    message: announcement.Title,
                    type: "Announcement",
                    referenceId: announcement.Id.ToString()
                );

                // Send email notification if user has opted in
                if (user.ReceiveEmailNotifications == true)
                {
                    // In a real app, you would call an email service here
                    // await _emailService.SendEmailAsync(user.Email, "New Announcement", emailContent);
                }

                // Send SMS notification if user has opted in
                if (user.ReceiveSmsNotifications == true && !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    // In a real app, you would call an SMS service here
                    // await _smsService.SendSmsAsync(user.PhoneNumber, smsContent);
                }
            }
        }
    }
}