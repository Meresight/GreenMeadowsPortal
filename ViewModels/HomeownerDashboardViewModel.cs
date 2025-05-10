using GreenMeadowsPortal.Models;
using System.Collections.Generic;

namespace GreenMeadowsPortal.ViewModels
{
    public class HomeownerDashboardViewModel
    {
        public ApplicationUser HomeownerUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalPropertiesOwned { get; set; }
        public int PendingRequests { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Added properties for notifications and announcements
        public int NotificationCount { get; set; }
        public List<AnnouncementDetailsViewModel> RecentAnnouncements { get; set; } = new List<AnnouncementDetailsViewModel>();

        // Payment summary
        public decimal TotalDue { get; set; }
        public int DaysUntilDue { get; set; }
        public bool HasOverduePayments { get; set; }

        // Upcoming events
        public int UpcomingEventsCount { get; set; }
        public List<EventViewModel> UpcomingEvents { get; set; } = new List<EventViewModel>();
    }

    // Simple event view model for the dashboard
    public class EventViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
    }
}