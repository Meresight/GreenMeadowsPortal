using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.Models
{
    // Main forum topic/thread model
    public class ForumTopic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastActivityDate { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey("AuthorId")]
        public ApplicationUser Author { get; set; } = null!;

        public int ViewCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;

        public bool IsPinned { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        // Navigation property for replies
        public virtual ICollection<ForumReply> Replies { get; set; } = new List<ForumReply>();
    }

    // Model for forum replies
    public class ForumReply
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? EditedDate { get; set; }

        [Required]
        public int TopicId { get; set; }

        [ForeignKey("TopicId")]
        public ForumTopic Topic { get; set; } = null!;

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey("AuthorId")]
        public ApplicationUser Author { get; set; } = null!;
    }

    // Model for reporting inappropriate content
    public class ForumReport
    {
        [Key]
        public int Id { get; set; }

        public int? TopicId { get; set; }

        public int? ReplyId { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [Required]
        public string ReportedById { get; set; } = string.Empty;

        [ForeignKey("ReportedById")]
        public ApplicationUser ReportedBy { get; set; } = null!;

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedDate { get; set; }

        public string? ResolvedById { get; set; }

        [ForeignKey("ResolvedById")]
        public ApplicationUser? ResolvedBy { get; set; }
    }
}