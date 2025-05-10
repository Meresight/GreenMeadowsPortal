using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Profile Picture URL")]
        public string ProfileImageUrl { get; set; } = string.Empty;

        [Display(Name = "Member Since")]
        public string MemberSince { get; set; } = string.Empty;

        [Display(Name = "Account Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "User Role")]
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

    }

    public class EmergencyContact
    {
        public string Name { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Skill
    {
        public string Name { get; set; } = string.Empty;
        public int ProficiencyLevel { get; set; } // 0–100
    }

    public class Training
    {
        public string Name { get; set; } = string.Empty;
        public DateTime CompletionDate { get; set; }
        public string CertificateUrl { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
    }

    public class WorkScheduleDay
    {
        public string DayOfWeek { get; set; } = string.Empty; // e.g. Monday
        public string ShiftName { get; set; } = string.Empty; // e.g. Morning, Night
        public string StartTime { get; set; } = string.Empty; // e.g. 08:00 AM
        public string EndTime { get; set; } = string.Empty;   // e.g. 05:00 PM
        public string Location { get; set; } = string.Empty;
        public string Assignments { get; set; } = string.Empty;
    }

    public class TimeOffRequest
    {
        public string Type { get; set; } = string.Empty; // e.g. Vacation, Sick Leave
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty; // e.g. Approved, Pending
        public string StatusClass { get; set; } = string.Empty; // used for CSS styling, e.g. "approved", "pending"
    }

    public class Accomplishment
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty; // FontAwesome icon class
        public DateTime Date { get; set; }
    }
}
