using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;

namespace GreenMeadowsPortal.ViewModels
{
    public class AdminDashboardViewModel
    {
        public ApplicationUser AdminUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveReservations { get; set; }
        public int PendingRequests { get; set; }
        public decimal OutstandingDues { get; set; }
        public int UpcomingEvents { get; set; }

        // Feedback statistics
        public int TotalFeedbacks { get; set; }
        public int NewFeedbacks { get; set; }
        public int ResolvedFeedbacks { get; set; }
        public List<FeedbackItemViewModel> RecentFeedbacks { get; set; } = new List<FeedbackItemViewModel>();

        // Recent activities
        public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();
    }

    // Activity view model for the dashboard
    public class ActivityViewModel
    {
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
    }
}