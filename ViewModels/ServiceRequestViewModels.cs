using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class ServiceRequestCreateViewModel
    {
        [Required(ErrorMessage = "Issue type is required")]
        [Display(Name = "Issue Type")]
        public string IssueType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Unit/Location")]
        public string Location { get; set; } = string.Empty;

        // Properties for layout and navigation
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
    }

    public class ServiceRequestDetailsViewModel
    {
        public int Id { get; set; }
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ServiceRequestStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterUnit { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public DateTime DateSubmitted { get; set; }
        public DateTime? DateClosed { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }

        // Properties for layout and navigation
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Permissions
        public bool CanClose { get; set; }
        public bool CanReopen { get; set; }
        public bool CanEdit { get; set; }
    }

    public class ServiceRequestListViewModel
    {
        public List<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
        public string StatusFilter { get; set; } = "all";

        // Properties for layout and navigation
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // For admin/staff view
        public int TotalOpenRequests { get; set; }
        public int TotalClosedRequests { get; set; }
    }
}