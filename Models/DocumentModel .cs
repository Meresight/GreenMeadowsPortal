using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreenMeadowsPortal.Models
{
    public class DocumentModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string VisibleTo { get; set; } = "All";

        [Required]
        [StringLength(255)]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(50)]
        public string FileType { get; set; } = string.Empty;

        [StringLength(20)]
        public string FileSize { get; set; } = string.Empty;

        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        public ApplicationUser UploadedBy { get; set; } = new ApplicationUser();

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}