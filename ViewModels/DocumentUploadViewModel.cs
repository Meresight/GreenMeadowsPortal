using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class DocumentUploadViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        [Required(ErrorMessage = "Document name is required")]
        [Display(Name = "Document Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify who can view this document")]
        [Display(Name = "Visible To")]
        public string VisibleTo { get; set; } = "All";

        [Required(ErrorMessage = "Please select a file to upload")]
        [Display(Name = "Document File")]
        public IFormFile? DocumentFile { get; set; }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Categories { get; set; } = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    }
}

