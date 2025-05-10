// ViewModels/PollViewModels.cs
using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class PollListViewModel
    {
        // User information for displaying in the layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Poll data
        public List<PollViewModel> ActivePolls { get; set; } = new List<PollViewModel>();
        public List<PollViewModel> CompletedPolls { get; set; } = new List<PollViewModel>();
        public string FilterCategory { get; set; } = string.Empty;
    }

    public class PollViewModel
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string TargetAudience { get; set; } = string.Empty;

        // Results data
        public int TotalResponses { get; set; }
        public int YesCount { get; set; }
        public int NoCount { get; set; }
        public double YesPercentage => TotalResponses > 0 ? Math.Round((double)YesCount / TotalResponses * 100, 1) : 0;
        public double NoPercentage => TotalResponses > 0 ? Math.Round((double)NoCount / TotalResponses * 100, 1) : 0;

        // User's response (if they've voted)
        public bool? UserResponse { get; set; }
        public bool HasVoted => UserResponse.HasValue;
    }

    public class PollCreateViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        [Required(ErrorMessage = "Question is required")]
        [StringLength(200, ErrorMessage = "Question cannot be longer than 200 characters")]
        public string Question { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Expiration Date")]
        [DataType(DataType.DateTime)]
        public DateTime? ExpirationDate { get; set; }

        [Display(Name = "Target Audience")]
        public string TargetAudience { get; set; } = "All";
    }

    public class PollDetailsViewModel : PollViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // More detailed user response data
        public List<PollResponseViewModel> RecentResponses { get; set; } = new List<PollResponseViewModel>();
    }

    public class PollResponseViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public bool Response { get; set; }
        public DateTime ResponseDate { get; set; }
        public string UserRole { get; set; } = string.Empty;
    }

    public class SubmitPollResponseViewModel
    {
        public int PollId { get; set; }
        public bool Response { get; set; }
    }
}