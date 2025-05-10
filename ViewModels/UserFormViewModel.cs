using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GreenMeadowsPortal.ViewModels
{
    public class UserFormViewModel
    {
        // User ID - null for new users
        public string Id { get; set; } = string.Empty;

        // Personal Information  
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        // Property Information  
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Unit/Apartment Number")]
        public string Unit { get; set; } = string.Empty;

        [Display(Name = "Property Type")]
        public string PropertyType { get; set; } = string.Empty;

        [Display(Name = "Ownership Status")]
        public string OwnershipStatus { get; set; } = string.Empty;

        // Account Information  
        [Required(ErrorMessage = "User role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Force password change on first login")]
        public bool ForcePasswordChange { get; set; } = true;

        [Display(Name = "Send account credentials via email")]
        public bool SendCredentials { get; set; } = true;

        // Staff-specific fields  
        // In UserFormViewModel.cs
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }

        [Display(Name = "Employee ID")]
        public string? EmployeeId { get; set; }

        // Homeowner-specific fields  
        [Display(Name = "Move-in Date")]
        [DataType(DataType.Date)]
        public DateTime? MoveInDate { get; set; }

        [Display(Name = "Emergency Contact Name")]
        public string EmergencyContactName { get; set; } = string.Empty;

        [Display(Name = "Emergency Contact Phone")]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        [Display(Name = "Vehicle Information")]
        public string VehicleInfo { get; set; } = string.Empty;

        [Display(Name = "Number of Residents")]
        [Range(1, 20, ErrorMessage = "Please enter a valid number of residents")]
        public int? ResidentCount { get; set; } = 1;

        // Additional Settings  
        [Display(Name = "Receive email notifications")]
        public bool ReceiveNotifications { get; set; } = true;

        [Display(Name = "Receive SMS notifications")]
        public bool ReceiveSMS { get; set; }

        [Display(Name = "Additional Notes")]
        public string Notes { get; set; } = string.Empty;

        // File Upload Properties  
        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        [Display(Name = "Property Documents")]
        public List<IFormFile> PropertyDocuments { get; set; } = new List<IFormFile>();

        // UI Helper Properties  
        public List<string> AvailableRoles { get; set; } = new List<string> { "Admin", "Staff", "Homeowner" };
        public int NotificationCount { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;
        public string CurrentUserProfileImageUrl { get; set; } = string.Empty;
    }

}