// ViewModels/EventViewModels.cs
using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class EventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDateTime { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public Models.EventType EventTypeEnum { get; set; }
        public EventStatus Status { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsAllDay { get; set; }
        public bool RequiresRegistration { get; set; }
        public int? MaxAttendees { get; set; }
        public int AttendeeCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedById { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string AttachmentUrl { get; set; } = string.Empty;
        public bool HasAvailableSpots { get; set; }
        public bool IsUserRegistered { get; set; } = false;

        // Formatting properties
        public string EventDateFormatted => EventDateTime.ToString("MMMM dd, yyyy");
        public string EventTimeFormatted
        {
            get
            {
                if (IsAllDay) return "All Day";
                if (!StartTime.HasValue) return "";
                var start = FormatTime(StartTime.Value);
                var end = EndTime.HasValue ? FormatTime(EndTime.Value) : "";
                return string.IsNullOrEmpty(end) ? start : $"{start} - {end}";
            }
        }

        private string FormatTime(TimeSpan time)
        {
            var dt = DateTime.Today.Add(time);
            return dt.ToString("h:mm tt");
        }
    }

    public class EventListViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Event data
        public List<EventViewModel> UpcomingEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> TodayEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> ThisWeekEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> ThisMonthEvents { get; set; } = new List<EventViewModel>();

        // Filtering and navigation
        public string SelectedView { get; set; } = "upcoming"; // upcoming, calendar, list
        public string SelectedCategory { get; set; } = "all";
        public string SelectedType { get; set; } = "all";
        public DateTime SelectedMonth { get; set; } = DateTime.Today;

        // User permissions
        public bool CanCreateEvents { get; set; }
        public bool CanEditEvents { get; set; }
    }

    public class EventCreateViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Event form fields
        [Required(ErrorMessage = "Event title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Event Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Event Date")]
        public DateTime EventDateTime { get; set; } = DateTime.Today;

        [DataType(DataType.Time)]
        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event type is required")]
        [Display(Name = "Event Type")]
        public Models.EventType EventTypeEnum { get; set; } = Models.EventType.Community;

        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "All Day Event")]
        public bool IsAllDay { get; set; } = false;

        [Display(Name = "Requires Registration")]
        public bool RequiresRegistration { get; set; } = false;

        [Display(Name = "Maximum Attendees")]
        [Range(1, 10000, ErrorMessage = "Please enter a valid number of attendees")]
        public int? MaxAttendees { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Display(Name = "Additional Notes")]
        public string Notes { get; set; } = string.Empty;

        [Display(Name = "Event Image")]
        public IFormFile? Image { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? Attachment { get; set; }
    }

    public class EventDetailsViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Event details
        public EventViewModel Event { get; set; } = new EventViewModel();
        public List<EventAttendeeViewModel> Attendees { get; set; } = new List<EventAttendeeViewModel>();

        // User permissions and status
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanManageAttendees { get; set; }
        public bool IsUserRegistered { get; set; }
        public bool CanRegister { get; set; }
        public bool RegistrationIsFull { get; set; }
    }

    public class EventAttendeeViewModel
    {
        public string AttendeeId { get; set; } = string.Empty;
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; }
        public AttendeeStatus Status { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
    }

    public class EventDashboardViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Event statistics
        public int TotalUpcomingEvents { get; set; }
        public List<EventViewModel> TodayEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> ThisWeekEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> ThisMonthEvents { get; set; } = new List<EventViewModel>();
        public List<EventViewModel> RecentEvents { get; set; } = new List<EventViewModel>();

        // Event categorization
        public Dictionary<string, int> EventsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EventsByCategory { get; set; } = new Dictionary<string, int>();

        // User permissions
        public bool CanCreateEvents { get; set; }
    }

    public class EventCalendarViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Calendar data
        public DateTime CurrentMonth { get; set; } = DateTime.Today;
        public List<EventViewModel> MonthEvents { get; set; } = new List<EventViewModel>();
        public Dictionary<int, List<EventViewModel>> CalendarDays { get; set; } = new Dictionary<int, List<EventViewModel>>();

        // Filtering options
        public string SelectedCategory { get; set; } = "all";
        public string SelectedType { get; set; } = "all";

        // User permissions
        public bool CanCreateEvents { get; set; }
    }
}