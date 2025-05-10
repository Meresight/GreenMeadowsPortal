using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class FeedbackListViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Feedback data
        public List<FeedbackItemViewModel> Submissions { get; set; } = new List<FeedbackItemViewModel>();
        public string StatusFilter { get; set; } = "all";
        public string TypeFilter { get; set; } = "all";
        public string SearchQuery { get; set; } = string.Empty;

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;

        // Statistics for admin/staff
        public int NewCount { get; set; }
        public int ResolvedCount { get; set; }
        public int TotalSubmissions { get; set; }
    }

    public class FeedbackItemViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeedbackStatus Status { get; set; }
        public string StatusColor => Status == FeedbackStatus.New ? "status-new" : "status-resolved";
        public string StatusText => Status.ToString();
        public DateTime SubmittedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;
        public string SubmittedByAvatar { get; set; } = "/images/default-avatar.png";
        public string? AdminResponse { get; set; }
        public string? ResolvedBy { get; set; }
        public FeedbackPriority Priority { get; set; }
        public string? Category { get; set; }
        public int ViewCount { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanRespond { get; set; }
    }

    public class FeedbackCreateViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Form fields
        [Required(ErrorMessage = "Type is required")]
        [Display(Name = "Feedback Type")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public string? Category { get; set; }

        // Dropdown options
        public List<SelectListItem> TypeOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Suggestion", Text = "Suggestion" },
            new SelectListItem { Value = "Complaint", Text = "Complaint" },
            new SelectListItem { Value = "General", Text = "General Feedback" }
        };

        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Select a category (optional)" },
            new SelectListItem { Value = "Maintenance", Text = "Maintenance" },
            new SelectListItem { Value = "Amenities", Text = "Amenities" },
            new SelectListItem { Value = "Security", Text = "Security" },
            new SelectListItem { Value = "Management", Text = "Management" },
            new SelectListItem { Value = "Community", Text = "Community" },
            new SelectListItem { Value = "Billing", Text = "Billing" },
            new SelectListItem { Value = "Other", Text = "Other" }
        };
    }

    public class FeedbackDetailsViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Feedback details
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public FeedbackStatus Status { get; set; }
        public string StatusColor => Status == FeedbackStatus.New ? "status-new" : "status-resolved";
        public string StatusText => Status.ToString();
        public DateTime SubmittedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public FeedbackPriority Priority { get; set; }
        public string? Category { get; set; }
        public int ViewCount { get; set; }

        // User information
        public string UserId { get; set; } = string.Empty;
        public string SubmittedBy { get; set; } = string.Empty;
        public string SubmittedByAvatar { get; set; } = "/images/default-avatar.png";
        public string SubmittedByEmail { get; set; } = string.Empty;

        // Admin information (if resolved)
        public string? AdminResponse { get; set; }
        public string? ResolvedBy { get; set; }
        public string? ResolvedByAvatar { get; set; }

        // Permissions
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanRespond { get; set; }
        public bool IsOwnSubmission { get; set; }

        // Admin response form
        [Display(Name = "Admin Response")]
        public string? NewAdminResponse { get; set; }
    }

    public class FeedbackDashboardViewModel
    {
        // User information for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Statistics
        public int NewFeedbackCount { get; set; }
        public int ResolvedFeedbackCount { get; set; }
        public int TotalFeedbackCount { get; set; }
        public int ComplaintsCount { get; set; }
        public int SuggestionsCount { get; set; }
        public double AverageResolutionTime { get; set; } // in hours
        public double SatisfactionRate { get; set; } // percentage

        // Recent submissions
        public List<FeedbackItemViewModel> RecentSubmissions { get; set; } = new List<FeedbackItemViewModel>();

        // Chart data
        public Dictionary<string, int> FeedbackByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> FeedbackByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<DateTime, int> FeedbackTrend { get; set; } = new Dictionary<DateTime, int>();
    }
}