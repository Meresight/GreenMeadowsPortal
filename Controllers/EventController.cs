// Controllers/EventController.cs
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly IEventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EventController> _logger;

        public EventController(
            IEventService eventService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment hostEnvironment,
            INotificationService notificationService,
            ILogger<EventController> logger)
        {
            _eventService = eventService;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            var viewModel = new EventListViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = userRole,
                CurrentUserId = user.Id,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                CanCreateEvents = roles.Contains("Admin"),
                CanEditEvents = roles.Contains("Admin"),
                UpcomingEvents = await _eventService.GetUpcomingEventsAsync(),
                TodayEvents = (await _eventService.GetUpcomingEventsAsync(DateTime.Today, DateTime.Today))
                    .Where(e => e.EventDateTime.Date == DateTime.Today).ToList(),
                ThisWeekEvents = await _eventService.GetEventsByDateRangeAsync(DateTime.Today, DateTime.Today.AddDays(7)),
                ThisMonthEvents = await _eventService.GetEventsByDateRangeAsync(
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1))
            };

            return View(viewModel);
        }

        // GET: Event/Calendar
        public async Task<IActionResult> Calendar(int? year, int? month)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            var currentMonth = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);

            var monthEvents = await _eventService.GetEventsByDateRangeAsync(currentMonth, nextMonth.AddDays(-1));

            // Group events by day for calendar display
            var calendarDays = new Dictionary<int, List<EventViewModel>>();
            foreach (var evt in monthEvents)
            {
                var day = evt.EventDateTime.Day;
                if (!calendarDays.ContainsKey(day))
                    calendarDays[day] = new List<EventViewModel>();
                calendarDays[day].Add(evt);
            }

            var viewModel = new EventCalendarViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = userRole,
                CurrentUserId = user.Id,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                CurrentMonth = currentMonth,
                MonthEvents = monthEvents,
                CalendarDays = calendarDays,
                CanCreateEvents = roles.Contains("Admin")
            };

            return View(viewModel);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "Homeowner";

            var eventViewModel = await _eventService.GetEventByIdAsync(id);
            if (eventViewModel == null)
                return NotFound();

            eventViewModel.IsUserRegistered = await _eventService.IsUserRegisteredForEventAsync(id, user.Id);

            var viewModel = new EventDetailsViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = userRole,
                CurrentUserId = user.Id,
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                Event = eventViewModel,
                Attendees = await _eventService.GetEventAttendeesAsync(id),
                CanEdit = roles.Contains("Admin"),
                CanDelete = roles.Contains("Admin"),
                CanManageAttendees = roles.Contains("Admin"),
                IsUserRegistered = eventViewModel.IsUserRegistered,
                CanRegister = eventViewModel.RequiresRegistration && !eventViewModel.IsUserRegistered,
                RegistrationIsFull = eventViewModel.MaxAttendees.HasValue && eventViewModel.AttendeeCount >= eventViewModel.MaxAttendees.Value
            };

            return View(viewModel);
        }

        // GET: Event/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EventCreateViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Admin",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            return View(viewModel);
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var roles = await _userManager.GetRolesAsync(user);
                model.FirstName = user.FirstName;
                model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                model.Role = roles.FirstOrDefault() ?? "Admin";
                model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            string? imageUrl = null;
            string? attachmentUrl = null;

            // Handle file uploads
            if (model.Image != null && model.Image.Length > 0)
            {
                imageUrl = await SaveFileAsync(model.Image, "images/events");
            }

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                attachmentUrl = await SaveFileAsync(model.Attachment, "attachments/events");
            }

            var eventModel = new EventModel
            {
                Title = model.Title,
                Description = model.Description,
                EventDateTime = model.EventDateTime,
                StartTime = model.IsAllDay ? null : model.StartTime,
                EndTime = model.IsAllDay ? null : model.EndTime,
                Location = model.Location,
                EventTypeEnum = model.EventTypeEnum,
                Category = model.Category,
                IsAllDay = model.IsAllDay,
                RequiresRegistration = model.RequiresRegistration,
                MaxAttendees = model.MaxAttendees,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                Notes = model.Notes,
                ImageUrl = imageUrl ?? string.Empty,
                AttachmentUrl = attachmentUrl ?? string.Empty,
                CreatedById = currentUser!.Id,
                Status = EventStatus.Scheduled
            };

            var eventId = await _eventService.CreateEventAsync(eventModel);

            TempData["SuccessMessage"] = "Event created successfully!";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }   

        // GET: Event/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var eventViewModel = await _eventService.GetEventByIdAsync(id);
            if (eventViewModel == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new EventCreateViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "Admin",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                Title = eventViewModel.Title,
                Description = eventViewModel.Description,
                EventDateTime = eventViewModel.EventDateTime,
                StartTime = eventViewModel.StartTime,
                EndTime = eventViewModel.EndTime,
                Location = eventViewModel.Location,
                EventTypeEnum = eventViewModel.EventTypeEnum,
                Category = eventViewModel.Category,
                IsAllDay = eventViewModel.IsAllDay,
                RequiresRegistration = eventViewModel.RequiresRegistration,
                MaxAttendees = eventViewModel.MaxAttendees,
                ContactEmail = eventViewModel.ContactEmail,
                ContactPhone = eventViewModel.ContactPhone,
                Notes = eventViewModel.Notes
            };

            return View(viewModel);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                var roles = await _userManager.GetRolesAsync(user);
                model.FirstName = user.FirstName;
                model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
                model.Role = roles.FirstOrDefault() ?? "Admin";
                model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var existingEvent = await _eventService.GetEventByIdAsync(id);
            if (existingEvent == null)
            {
                return NotFound();
            }

            string imageUrl = existingEvent.ImageUrl;
            string attachmentUrl = existingEvent.AttachmentUrl;

            // Handle file uploads
            if (model.Image != null && model.Image.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingEvent.ImageUrl))
                {
                    DeleteFile(existingEvent.ImageUrl);
                }
                imageUrl = await SaveFileAsync(model.Image, "images/events");
            }

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                // Delete old attachment if exists
                if (!string.IsNullOrEmpty(existingEvent.AttachmentUrl))
                {
                    DeleteFile(existingEvent.AttachmentUrl);
                }
                attachmentUrl = await SaveFileAsync(model.Attachment, "attachments/events");
            }

            var eventModel = new EventModel
            {
                Id = id,
                Title = model.Title,
                Description = model.Description,
                EventDateTime = model.EventDateTime,
                StartTime = model.IsAllDay ? null : model.StartTime,
                EndTime = model.IsAllDay ? null : model.EndTime,
                Location = model.Location,
                EventTypeEnum = model.EventTypeEnum,
                Category = model.Category,
                IsAllDay = model.IsAllDay,
                RequiresRegistration = model.RequiresRegistration,
                MaxAttendees = model.MaxAttendees,
                ContactEmail = model.ContactEmail,
                ContactPhone = model.ContactPhone,
                Notes = model.Notes,
                ImageUrl = imageUrl,
                AttachmentUrl = attachmentUrl,
                CreatedById = existingEvent.CreatedById,
                CreatedDate = existingEvent.CreatedDate,
                LastModifiedById = currentUser.Id,
                LastModifiedDate = DateTime.Now,
                Status = existingEvent.Status
            };

            var success = await _eventService.UpdateEventAsync(eventModel);

            if (success)
            {
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }

            ModelState.AddModelError("", "Failed to update event. Please try again.");
            return View(model);
        }

        // GET: Event/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var eventViewModel = await _eventService.GetEventByIdAsync(id);
            if (eventViewModel == null)
                return NotFound();

            return View(eventViewModel);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eventViewModel = await _eventService.GetEventByIdAsync(id);
            if (eventViewModel == null)
                return NotFound();

            // Delete associated files
            if (!string.IsNullOrEmpty(eventViewModel.ImageUrl))
            {
                DeleteFile(eventViewModel.ImageUrl);
            }

            if (!string.IsNullOrEmpty(eventViewModel.AttachmentUrl))
            {
                DeleteFile(eventViewModel.AttachmentUrl);
            }

            var success = await _eventService.DeleteEventAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete event. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Event/Register/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var success = await _eventService.RegisterForEventAsync(id, user.Id);

            if (success)
            {
                TempData["SuccessMessage"] = "You have successfully registered for this event!";
            }
            else
            {
                TempData["ErrorMessage"] = "Registration failed. The event may be full or you may already be registered.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Event/Unregister/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var success = await _eventService.UnregisterFromEventAsync(id, user.Id);

            if (success)
            {
                TempData["SuccessMessage"] = "You have successfully unregistered from this event.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to unregister. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Event/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var success = await _eventService.CancelEventAsync(id, reason);

            if (success)
            {
                TempData["SuccessMessage"] = "Event cancelled successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to cancel event. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Event/MarkComplete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var success = await _eventService.MarkEventCompleteAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Event marked as completed!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update event status. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Helper Methods
        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return string.Empty;

                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, subfolder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Path.GetFileName(file.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return $"/{subfolder}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", file?.FileName);
                return string.Empty;
            }
        }

        private void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            try
            {
                var relativePath = fileUrl.TrimStart('/');
                var fullPath = Path.Combine(_hostEnvironment.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            }
        }
    }
}