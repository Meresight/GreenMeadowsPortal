// Models/EventModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class EventModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EventDateTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public EventType EventTypeEnum { get; set; } = EventType.Community;

        [Required]
        public EventStatus Status { get; set; } = EventStatus.Scheduled;

        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // Community, Maintenance, Recreation, etc.

        public bool IsAllDay { get; set; } = false;

        public bool RequiresRegistration { get; set; } = false;

        public int? MaxAttendees { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }

        public string? LastModifiedById { get; set; }

        [ForeignKey("LastModifiedById")]
        public ApplicationUser? LastModifiedBy { get; set; }

        // Contact information
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone]
        public string ContactPhone { get; set; } = string.Empty;

        // Additional details
        public string Notes { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string AttachmentUrl { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    }

    public enum EventType
    {
        Community,
        Maintenance,
        Recreation,
        Meeting,
        Social,
        Holiday,
        Emergency,
        Announcement
    }

    public enum EventStatus
    {
        Scheduled,
        Ongoing,
        Completed,
        Cancelled,
        Postponed
    }

    public class EventAttendee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public EventModel Event { get; set; } = null!;

        [Required]
        public string AttendeeId { get; set; } = string.Empty;

        [ForeignKey("AttendeeId")]
        public ApplicationUser Attendee { get; set; } = null!;

        public DateTime RegisteredDate { get; set; } = DateTime.Now;

        public string Notes { get; set; } = string.Empty;

        public AttendeeStatus Status { get; set; } = AttendeeStatus.Registered;
    }

    public enum AttendeeStatus
    {
        Registered,
        Attended,
        NoShow,
        Cancelled
    }
}