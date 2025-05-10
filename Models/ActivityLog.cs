// Models/ActivityLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = new ApplicationUser();

        [Required]
        public string ActivityType { get; set; } = string.Empty;

        [Required]
        public string Details { get; set; } = string.Empty;

        [Required]
        public string IpAddress { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}