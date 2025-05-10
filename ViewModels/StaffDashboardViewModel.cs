using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;

namespace GreenMeadowsPortal.ViewModels
{
    public class StaffDashboardViewModel
    {
        public ApplicationUser StaffUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;

        public int NotificationCount { get; set; }
        public int TotalResidents { get; set; }
        public int PendingRequests { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string StaffRole { get; set; } = string.Empty;
        public decimal DuePaymentsTotal { get; set; }
        public int UpcomingEvents { get; set; }

        // Feedback statistics
        public int TotalFeedbacks { get; set; }
        public int NewFeedbacks { get; set; }
        public int ResolvedFeedbacks { get; set; }
        public List<FeedbackItemViewModel> RecentFeedbacks { get; set; } = new List<FeedbackItemViewModel>();

        // Recent activities for dashboard
        public List<DashboardServiceRequest> RecentServiceRequests { get; set; } = new List<DashboardServiceRequest>();
        public List<DashboardUser> RecentUsers { get; set; } = new List<DashboardUser>();
        public List<Event> UpcomingEventsList { get; set; } = new List<Event>();

        // Daily statistics
        public int DailyActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int TotalLoginToday { get; set; }
        public int RequestsCreatedToday { get; set; }
        public int RequestsCompletedToday { get; set; }
        public double AverageResolutionTime { get; set; }
        public int FacilityBookingsToday { get; set; }
        public string MostBookedFacility { get; set; } = string.Empty;
        public int UpcomingBookings { get; set; }
    }

    // Service request model for the dashboard
    public class DashboardServiceRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string PriorityClass { get; set; } = string.Empty;
    }

    // User model for the dashboard activity
    public class DashboardUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public string ProfileImageUrl { get; set; } = "/images/default-avatar.png";
        public string ActivityDescription { get; set; } = string.Empty;
        public string ActivityTime { get; set; } = string.Empty;
    }

    // Event model for the dashboard
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}