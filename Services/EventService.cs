// Services/EventService.cs
using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public interface IEventService
    {
        Task<List<EventViewModel>> GetUpcomingEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<EventViewModel>> GetAllEventsAsync();
        Task<EventViewModel?> GetEventByIdAsync(int id);
        Task<int> CreateEventAsync(EventModel eventModel);
        Task<bool> UpdateEventAsync(EventModel eventModel);
        Task<bool> DeleteEventAsync(int id);
        Task<bool> CancelEventAsync(int id, string reason);
        Task<bool> MarkEventCompleteAsync(int id);
        Task<bool> RegisterForEventAsync(int eventId, string userId);
        Task<bool> UnregisterFromEventAsync(int eventId, string userId);
        Task<bool> IsUserRegisteredForEventAsync(int eventId, string userId);
        Task<List<EventViewModel>> GetEventsByTypeAsync(EventType eventType);
        Task<List<EventViewModel>> GetEventsByCategoryAsync(string category);
        Task<List<EventViewModel>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<EventDashboardViewModel> GetEventDashboardDataAsync(string userRole);
        Task<List<EventAttendeeViewModel>> GetEventAttendeesAsync(int eventId);
    }

    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EventService> _logger;

        public EventService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<EventService> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<EventViewModel>> GetUpcomingEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today;
                var end = endDate ?? DateTime.Today.AddMonths(3);

                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .Where(e => e.EventDateTime >= start && e.EventDateTime <= end && e.Status != EventStatus.Cancelled)
                    .OrderBy(e => e.EventDateTime)
                    .ThenBy(e => e.StartTime)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                return new List<EventViewModel>();
            }
        }

        public async Task<List<EventViewModel>> GetAllEventsAsync()
        {
            try
            {
                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .OrderByDescending(e => e.EventDateTime)
                    .ThenByDescending(e => e.StartTime)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all events");
                return new List<EventViewModel>();
            }
        }

        public async Task<EventViewModel?> GetEventByIdAsync(int id)
        {
            try
            {
                var eventModel = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .ThenInclude(a => a.Attendee)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (eventModel == null)
                    return null;

                return MapToEventViewModel(eventModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event by ID: {EventId}", id);
                return null;
            }
        }

        public async Task<int> CreateEventAsync(EventModel eventModel)
        {
            try
            {
                _context.EventModels.Add(eventModel);
                await _context.SaveChangesAsync();

                // Send notifications for new events
                await SendEventNotifications(eventModel, "New Event Created");

                return eventModel.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }

        public async Task<bool> UpdateEventAsync(EventModel eventModel)
        {
            try
            {
                _context.Entry(eventModel).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Send notifications for updated events
                await SendEventNotifications(eventModel, "Event Updated");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event: {EventId}", eventModel.Id);
                return false;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var eventModel = await _context.EventModels.FindAsync(id);
                if (eventModel == null)
                    return false;

                _context.EventModels.Remove(eventModel);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event: {EventId}", id);
                return false;
            }
        }

        public async Task<bool> CancelEventAsync(int id, string reason)
        {
            try
            {
                var eventModel = await _context.EventModels.FindAsync(id);
                if (eventModel == null)
                    return false;

                eventModel.Status = EventStatus.Cancelled;
                eventModel.Notes += $"\nCancelled: {reason}";

                await _context.SaveChangesAsync();

                // Notify all registered attendees
                await SendEventNotifications(eventModel, "Event Cancelled", reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event: {EventId}", id);
                return false;
            }
        }

        public async Task<bool> MarkEventCompleteAsync(int id)
        {
            try
            {
                var eventModel = await _context.EventModels.FindAsync(id);
                if (eventModel == null)
                    return false;

                eventModel.Status = EventStatus.Completed;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking event complete: {EventId}", id);
                return false;
            }
        }

        public async Task<bool> RegisterForEventAsync(int eventId, string userId)
        {
            try
            {
                // Check if already registered
                var existingRegistration = await _context.EventAttendees
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.AttendeeId == userId);

                if (existingRegistration != null)
                    return false;

                // Check event capacity
                var eventModel = await _context.EventModels
                    .Include(e => e.Attendees)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (eventModel == null)
                    return false;

                if (eventModel.MaxAttendees.HasValue && eventModel.Attendees.Count >= eventModel.MaxAttendees.Value)
                    return false;

                var attendee = new EventAttendee
                {
                    EventId = eventId,
                    AttendeeId = userId,
                    RegisteredDate = DateTime.Now,
                    Status = AttendeeStatus.Registered
                };

                _context.EventAttendees.Add(attendee);
                await _context.SaveChangesAsync();

                // Send confirmation notification
                await _notificationService.CreateNotificationAsync(
                    userId,
                    "Event Registration Confirmed",
                    $"You have successfully registered for {eventModel.Title}",
                    "Event",
                    eventModel.Id.ToString()
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event: {EventId}, User: {UserId}", eventId, userId);
                return false;
            }
        }

        public async Task<bool> UnregisterFromEventAsync(int eventId, string userId)
        {
            try
            {
                var attendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(a => a.EventId == eventId && a.AttendeeId == userId);

                if (attendee == null)
                    return false;

                _context.EventAttendees.Remove(attendee);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering from event: {EventId}, User: {UserId}", eventId, userId);
                return false;
            }
        }

        public async Task<bool> IsUserRegisteredForEventAsync(int eventId, string userId)
        {
            try
            {
                return await _context.EventAttendees
                    .AnyAsync(a => a.EventId == eventId && a.AttendeeId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status: {EventId}, User: {UserId}", eventId, userId);
                return false;
            }
        }

        public async Task<List<EventViewModel>> GetEventsByTypeAsync(EventType eventType)
        {
            try
            {
                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .Where(e => e.EventTypeEnum == eventType && e.Status != EventStatus.Cancelled)
                    .OrderBy(e => e.EventDateTime)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by type: {EventType}", eventType);
                return new List<EventViewModel>();
            }
        }

        public async Task<List<EventViewModel>> GetEventsByCategoryAsync(string category)
        {
            try
            {
                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .Where(e => e.Category == category && e.Status != EventStatus.Cancelled)
                    .OrderBy(e => e.EventDateTime)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by category: {Category}", category);
                return new List<EventViewModel>();
            }
        }

        public async Task<List<EventViewModel>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .Where(e => e.EventDateTime >= startDate && e.EventDateTime <= endDate)
                    .OrderBy(e => e.EventDateTime)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by date range");
                return new List<EventViewModel>();
            }
        }

        public async Task<EventDashboardViewModel> GetEventDashboardDataAsync(string userRole)
        {
            try
            {
                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var nextMonth = thisMonth.AddMonths(1);

                var upcomingEvents = await GetUpcomingEventsAsync(today, today.AddMonths(1));
                var todayEvents = upcomingEvents.Where(e => e.EventDateTime.Date == today).ToList();
                var thisWeekEvents = upcomingEvents.Where(e => e.EventDateTime >= today && e.EventDateTime <= today.AddDays(7)).ToList();
                var thisMonthEvents = upcomingEvents.Where(e => e.EventDateTime >= thisMonth && e.EventDateTime < nextMonth).ToList();

                var dashboard = new EventDashboardViewModel
                {
                    TodayEvents = todayEvents,
                    ThisWeekEvents = thisWeekEvents,
                    ThisMonthEvents = thisMonthEvents,
                    TotalUpcomingEvents = upcomingEvents.Count,
                    EventsByType = upcomingEvents.GroupBy(e => e.EventTypeEnum)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    EventsByCategory = upcomingEvents.GroupBy(e => e.Category)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    RecentEvents = await GetRecentEventsAsync(5),
                    CanCreateEvents = userRole == "Admin"
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event dashboard data");
                return new EventDashboardViewModel();
            }
        }

        public async Task<List<EventAttendeeViewModel>> GetEventAttendeesAsync(int eventId)
        {
            try
            {
                var attendees = await _context.EventAttendees
                    .Include(a => a.Attendee)
                    .Where(a => a.EventId == eventId)
                    .OrderBy(a => a.RegisteredDate)
                    .ToListAsync();

                return attendees.Select(a => new EventAttendeeViewModel
                {
                    AttendeeId = a.AttendeeId,
                    AttendeeName = $"{a.Attendee.FirstName} {a.Attendee.LastName}",
                    AttendeeEmail = a.Attendee.Email ?? string.Empty,
                    RegisteredDate = a.RegisteredDate,
                    Status = a.Status,
                    Notes = a.Notes,
                    ProfileImageUrl = a.Attendee.ProfileImageUrl ?? "/images/default-avatar.png"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event attendees: {EventId}", eventId);
                return new List<EventAttendeeViewModel>();
            }
        }

        private async Task<List<EventViewModel>> GetRecentEventsAsync(int count)
        {
            try
            {
                var today = DateTime.Today;
                var events = await _context.EventModels
                    .Include(e => e.CreatedBy)
                    .Include(e => e.Attendees)
                    .Where(e => e.EventDateTime < today && e.Status == EventStatus.Completed)
                    .OrderByDescending(e => e.EventDateTime)
                    .Take(count)
                    .ToListAsync();

                return MapToEventViewModels(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent events");
                return new List<EventViewModel>();
            }
        }

        private List<EventViewModel> MapToEventViewModels(List<EventModel> events)
        {
            return events.Select(MapToEventViewModel).ToList();
        }

        private EventViewModel MapToEventViewModel(EventModel eventModel)
        {
            return new EventViewModel
            {
                Id = eventModel.Id,
                Title = eventModel.Title,
                Description = eventModel.Description,
                EventDateTime = eventModel.EventDateTime,
                StartTime = eventModel.StartTime,
                EndTime = eventModel.EndTime,
                Location = eventModel.Location,
                EventTypeEnum = eventModel.EventTypeEnum,
                Status = eventModel.Status,
                Category = eventModel.Category,
                IsAllDay = eventModel.IsAllDay,
                RequiresRegistration = eventModel.RequiresRegistration,
                MaxAttendees = eventModel.MaxAttendees,
                AttendeeCount = eventModel.Attendees.Count,
                CreatedBy = eventModel.CreatedBy.FirstName + " " + eventModel.CreatedBy.LastName,
                CreatedById = eventModel.CreatedById,
                CreatedDate = eventModel.CreatedDate,
                ContactEmail = eventModel.ContactEmail,
                ContactPhone = eventModel.ContactPhone,
                Notes = eventModel.Notes,
                ImageUrl = eventModel.ImageUrl,
                AttachmentUrl = eventModel.AttachmentUrl,
                HasAvailableSpots = !eventModel.MaxAttendees.HasValue || eventModel.Attendees.Count < eventModel.MaxAttendees.Value
            };
        }

        private async Task SendEventNotifications(EventModel eventModel, string notificationType, string? extraMessage = null)
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var message = notificationType == "Event Cancelled"
                    ? $"Event '{eventModel.Title}' has been cancelled. {extraMessage}"
                    : $"Event '{eventModel.Title}' on {eventModel.EventDateTime:MMMM dd, yyyy}";

                foreach (var user in users)
                {
                    await _notificationService.CreateNotificationAsync(
                        user.Id,
                        notificationType,
                        message,
                        "Event",
                        eventModel.Id.ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event notifications");
            }
        }
    }
}