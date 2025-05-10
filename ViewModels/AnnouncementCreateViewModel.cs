// ViewModels/AnnouncementViewModels.cs
using GreenMeadowsPortal.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class AnnouncementCreateViewModel
    {
        [Required]
        [StringLength(150, MinimumLength = 5)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Publish Date")]
        [DataType(DataType.DateTime)]
        public DateTime? PublishDate { get; set; }

        [Display(Name = "Expiration Date")]
        [DataType(DataType.DateTime)]
        public DateTime? ExpirationDate { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.General;

        [Required]
        [Display(Name = "Status")]
        public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;

        [Display(Name = "Category")]
        public string CategoryId { get; set; } = string.Empty;

        [Display(Name = "Target Audience")]
        public string TargetAudience { get; set; } = "All";

        [Display(Name = "Attachment")]
        public IFormFile? Attachment { get; set; }

        [Display(Name = "Image")]
        public IFormFile? Image { get; set; }

        // Added properties to fix the errors
        public string Role { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public bool SaveAsDraft { get; set; }
        public string ProfileImageUrl { get; set; } = string.Empty;
    }

    public class AnnouncementEditViewModel : AnnouncementCreateViewModel
    {
        public int Id { get; set; }
        public string ExistingAttachmentUrl { get; set; } = string.Empty;
        public string ExistingImageUrl { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class AnnouncementDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public AnnouncementPriority Priority { get; set; }
        public AnnouncementStatus Status { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string AttachmentUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool HasBeenRead { get; set; }
        public int ReadCount { get; set; }
        public List<AnnouncementReadReceiptViewModel> ReadReceipts { get; set; } = new List<AnnouncementReadReceiptViewModel>();
    }

    public class AnnouncementReadReceiptViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime ReadDate { get; set; }
    }

    public class AnnouncementListViewModel
    {
        public List<AnnouncementDetailsViewModel> Announcements { get; set; } = new List<AnnouncementDetailsViewModel>();
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
        public string FilterCategory { get; set; } = string.Empty;
        public string FilterPriority { get; set; } = string.Empty;
        public string SearchQuery { get; set; } = string.Empty;
        public bool ShowExpired { get; set; }
        public int TotalCount { get; set; }
    }

    public class AnnouncementDashboardViewModel
    {
        public List<AnnouncementDetailsViewModel> RecentAnnouncements { get; set; } = new List<AnnouncementDetailsViewModel>();
        public int TotalAnnouncements { get; set; }
        public int UnreadAnnouncements { get; set; }
        public int UrgentAnnouncements { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
    }
}
