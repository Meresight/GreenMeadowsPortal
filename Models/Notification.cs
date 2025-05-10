using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; } // Added 'required' modifier

        [ForeignKey("UserId")]
        public required ApplicationUser User { get; set; } // Added 'required' modifier

        [Required]
        [StringLength(100)]
        public required string Title { get; set; } // Added 'required' modifier

        [Required]
        [StringLength(500)]
        public required string Message { get; set; } // Added 'required' modifier

        [Required]
        [StringLength(50)]
        public required string Type { get; set; } // Added 'required' modifier

        // Optional reference to related item (e.g., announcement ID, service request ID)
        public string? ReferenceId { get; set; } // Made nullable

        [Required]
        public bool IsRead { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
