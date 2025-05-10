using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.ViewModels
{
    // Main view model for forum index page
    public class ForumIndexViewModel
    {
        // Common properties for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Forum specific properties
        public List<ForumTopicViewModel> Topics { get; set; } = new List<ForumTopicViewModel>();
        public int TotalTopics { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "recent";
        public string SearchQuery { get; set; } = string.Empty;
        public bool CanCreateNewTopic { get; set; } = true;
        public bool IsAdmin { get; set; } = false;
        public bool IsStaff { get; set; } = false;
    }

    // View model for individual forum topics in the list
    public class ForumTopicViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorProfileImage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsClosed { get; set; }
        public bool CanModerate { get; set; }
    }

    // View model for creating a new forum topic
    public class CreateTopicViewModel
    {
        // Common properties for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Form fields
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [MinLength(10, ErrorMessage = "Content must be at least 10 characters")]
        public string Content { get; set; } = string.Empty;
    }

    // View model for topic details page with replies
    public class TopicDetailsViewModel
    {
        // Common properties for layout
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Topic details
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorProfileImage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int ViewCount { get; set; }
        public int ReplyCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsClosed { get; set; }

        // Replies
        public List<ReplyViewModel> Replies { get; set; } = new List<ReplyViewModel>();

        // Reply form
        [Required(ErrorMessage = "Reply content is required")]
        [MinLength(5, ErrorMessage = "Reply must be at least 5 characters")]
        public string NewReplyContent { get; set; } = string.Empty;

        // Permissions
        public bool CanReply { get; set; } = true;
        public bool CanModerate { get; set; } = false;
        public bool CanPin { get; set; } = false;
        public bool CanClose { get; set; } = false;
    }

    // View model for individual replies
    public class ReplyViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorProfileImage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? EditedDate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool IsEditing { get; set; } = false;
    }

    // Report form view model
    public class ReportContentViewModel
    {
        public int? TopicId { get; set; }
        public int? ReplyId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for reporting")]
        [MinLength(10, ErrorMessage = "Reason must be at least 10 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}