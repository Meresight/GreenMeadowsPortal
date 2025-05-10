using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Issue type is required")]
        [Display(Name = "Issue Type")]
        public string IssueType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Open;

        [Required]
        [Display(Name = "Submitted By")]
        public string RequesterId { get; set; } = string.Empty;

        [ForeignKey("RequesterId")]
        public ApplicationUser? Requester { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }

        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; } = DateTime.Now;

        [Display(Name = "Date Assigned")]
        public DateTime? DateAssigned { get; set; }

        [Display(Name = "Date Closed")]
        public DateTime? DateClosed { get; set; }

        [Display(Name = "Unit/Location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }
    }

    public enum ServiceRequestStatus
    {
        Open,
        Closed
    }
}