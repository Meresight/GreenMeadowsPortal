using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Suggestion, Complaint, General

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

        [Required]
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public DateTime? ResolvedDate { get; set; }

        public string? AdminResponse { get; set; }

        public string? AdminUserId { get; set; }

        [ForeignKey("AdminUserId")]
        public ApplicationUser? AdminUser { get; set; }

        // Additional properties for tracking
        public int ViewCount { get; set; } = 0;
        public FeedbackPriority Priority { get; set; } = FeedbackPriority.Normal;
        public string? Category { get; set; } // e.g., Maintenance, Amenities, Security, etc.
    }

    public enum FeedbackStatus
    {
        New,
        Resolved
    }

    public enum FeedbackPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
}