using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class AdminAnnouncement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? PublishDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey("AuthorId")]
        public ApplicationUser Author { get; set; } = null!;

        [Required]
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.General;

        [Required]
        public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Draft;

        public string CategoryId { get; set; } = string.Empty;

        [ForeignKey("CategoryId")]
        public AnnouncementCategory Category { get; set; } = null!;

        public string TargetAudience { get; set; } = string.Empty; // Can be "All", "Staff", "Homeowners", or specific area IDs

        public string AttachmentUrl { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        // Navigation property for read tracking
        public virtual ICollection<AnnouncementReadReceipt> ReadReceipts { get; set; } = new List<AnnouncementReadReceipt>();
    }

    public enum AnnouncementPriority
    {
        Urgent,
        Important,
        General
    }

    public enum AnnouncementStatus
    {
        Draft,
        Published,
        Archived
    }

    public class AnnouncementCategory
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public virtual ICollection<AdminAnnouncement> Announcements { get; set; } = new List<AdminAnnouncement>();
    }

    public class AnnouncementReadReceipt
    {
        [Key]
        public int Id { get; set; }

        public int AnnouncementId { get; set; }

        [ForeignKey("AnnouncementId")]
        public AdminAnnouncement Announcement { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;

        public DateTime ReadDate { get; set; } = DateTime.Now;
    }
}
