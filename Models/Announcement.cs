using System;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty; // Fix for CS8618

        [Required]
        public string Content { get; set; } = string.Empty; // Fix for CS8618

        [Required]
        public string Type { get; set; } = string.Empty; // Fix for CS8618

        public DateTime Date { get; set; } = DateTime.Now;

        public string PostedBy { get; set; } = string.Empty; // Fix for CS8618

        public bool IsActive { get; set; } = true;

        public string ImageUrl { get; set; } = string.Empty; // Fix for CS8618
    }
}
