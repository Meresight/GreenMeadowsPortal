// Models/Poll.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.Models
{
    public class Poll
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ExpirationDate { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey("CreatedById")]
        public ApplicationUser CreatedBy { get; set; } = new ApplicationUser();

        [Required]
        public bool IsActive { get; set; } = true;

        public string Description { get; set; } = string.Empty;

        public string TargetAudience { get; set; } = "All"; // All, Homeowners, Staff

        // Navigation property for responses
        public virtual ICollection<PollResponse> Responses { get; set; } = new List<PollResponse>();
    }

    public class PollResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PollId { get; set; }

        [ForeignKey("PollId")]
        public Poll? Poll { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        [Required]
        public bool Response { get; set; } // Yes = true, No = false

        [Required]
        public DateTime ResponseDate { get; set; } = DateTime.Now;
    }
}
