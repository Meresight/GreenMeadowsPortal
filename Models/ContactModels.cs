using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    // Contact Category (Departments, Emergency, etc)
    public class ContactCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public bool IsPublic { get; set; } = true;

        // Order for display purposes
        public int DisplayOrder { get; set; } = 0;
    }

    // Department contacts
    public class DepartmentContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string DepartmentName { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string OfficeHours { get; set; } = string.Empty;

        [StringLength(50)]
        public string Location { get; set; } = string.Empty;

        // For reference to actual user if applicable
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }

    // Emergency contacts
    public class EmergencyContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        // Priority for display (1 = highest)
        [Range(1, 10)]
        public int Priority { get; set; } = 5;
    }

    // Vendor contacts
    public class VendorContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Service { get; set; } = string.Empty;

        [StringLength(200)]
        public string Website { get; set; } = string.Empty;

        [StringLength(200)]
        public string Notes { get; set; } = string.Empty;

        // Is preferred vendor
        public bool IsPreferred { get; set; } = false;
    }

    // Community contact (residents who choose to be visible in directory)
    public class CommunityContact
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = new ApplicationUser();

        // Visibility settings
        public bool ShowPhoneNumber { get; set; } = false;
        public bool ShowEmail { get; set; } = true;
        public bool ShowAddress { get; set; } = false;

        // Additional info
        [StringLength(200)]
        public string Bio { get; set; } = string.Empty;

        // If false, only directory admins can see
        public bool IsPublic { get; set; } = true;
    }

    // Contact messages
    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public ApplicationUser Sender { get; set; } = new ApplicationUser();

        [Required]
        public string RecipientId { get; set; } = string.Empty;

        [ForeignKey("RecipientId")]
        public ApplicationUser Recipient { get; set; } = new ApplicationUser();

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public DateTime SentDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadDate { get; set; }

        // Deletion flags (soft delete implementation)
        public bool DeletedBySender { get; set; } = false;
        public bool DeletedByRecipient { get; set; } = false;
    }
}