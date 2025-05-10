using GreenMeadowsPortal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.ViewModels
{
    // Main view model for contact directory
    public class ContactDirectoryViewModel
    {
        // User information for displaying in the layout
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Contact directory data
        public List<ContactCategory> ContactCategories { get; set; } = new List<ContactCategory>();
        public List<DepartmentContactViewModel> DepartmentContacts { get; set; } = new List<DepartmentContactViewModel>();
        public List<EmergencyContactViewModel> EmergencyContacts { get; set; } = new List<EmergencyContactViewModel>();
        public List<VendorContactViewModel> VendorContacts { get; set; } = new List<VendorContactViewModel>();
        public List<CommunityContactViewModel> StaffContacts { get; set; } = new List<CommunityContactViewModel>();
        public List<CommunityContactViewModel> CommunityContacts { get; set; } = new List<CommunityContactViewModel>();
    }

    // Department contact view model
    public class DepartmentContactViewModel
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OfficeHours { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
    }

    // Emergency contact view model
    public class EmergencyContactViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    // Vendor contact view model
    public class VendorContactViewModel
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
    }

    // Community contact view model
    public class CommunityContactViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public bool ShowEmail { get; set; }
        public bool ShowPhoneNumber { get; set; }
        public bool ShowAddress { get; set; }
        public bool CanMessage { get; set; } = false;

    }

    // View model for sending a message
    public class ContactMessageViewModel
    {
        // User information for displaying in the layout
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();
        public ApplicationUser ContactUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Contact information
        public string ContactId { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactRole { get; set; } = string.Empty;
        public string ContactDepartment { get; set; } = string.Empty;
        public string ContactImageUrl { get; set; } = string.Empty;

        // Message details
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message content is required")]
        public string Message { get; set; } = string.Empty;
    }

    // View model for inbox
    public class ContactInboxViewModel
    {
        // User information for displaying in the layout
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Message listings
        public List<ContactMessageListingViewModel> Messages { get; set; } = new List<ContactMessageListingViewModel>();

        // Role-specific customizations
        public string InboxTitle { get; set; } = "Message Inbox";
        public bool CanManageAllMessages { get; set; } = false;
        public bool ShowStaffSection { get; set; } = false;
        public bool ShowDepartmentSection { get; set; } = false;
    }

    // View model for message listing in inbox
    public class ContactMessageListingViewModel
    {
        public int MessageId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsFromCurrentUser { get; set; }

        // Sender details
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderProfileImage { get; set; } = string.Empty;

        // Recipient details
        public string RecipientId { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientProfileImage { get; set; } = string.Empty;
    }

    // View model for viewing a message
    // Let's complete the ViewMessageViewModel class that was cut off
    public class ViewMessageViewModel
    {
        // User information for displaying in the layout
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Message details
        public int MessageId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string MessageContent { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }

        // Sender details
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string SenderImageUrl { get; set; } = string.Empty;

        // Recipient details
        public string RecipientId { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string RecipientRole { get; set; } = string.Empty;
        public string RecipientImageUrl { get; set; } = string.Empty;
    }

    // Additional ViewModel for Contact Management by Admin
    public class ManageContactsViewModel
    {
        // User information for displaying in the layout
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Contact data for management
        public List<ContactCategory> ContactCategories { get; set; } = new List<ContactCategory>();
        public List<DepartmentContactViewModel> DepartmentContacts { get; set; } = new List<DepartmentContactViewModel>();
        public List<EmergencyContactViewModel> EmergencyContacts { get; set; } = new List<EmergencyContactViewModel>();
        public List<VendorContactViewModel> VendorContacts { get; set; } = new List<VendorContactViewModel>();
        public List<CommunityContactViewModel> CommunityContacts { get; set; } = new List<CommunityContactViewModel>();
        public List<ApplicationUser> AvailableUsers { get; set; } = new List<ApplicationUser>();
        public AddContactCategoryViewModel AddCategoryModel { get; set; } = new AddContactCategoryViewModel();
        public AddVendorContactViewModel AddVendorModel { get; set; } = new AddVendorContactViewModel();
        public AddDepartmentContactViewModel AddDepartmentModel { get; set; } = new AddDepartmentContactViewModel();
        public AddCommunityContactViewModel AddCommunityModel { get; set; } = new AddCommunityContactViewModel();
    }

    // ViewModel for adding a contact category
    public class AddContactCategoryViewModel
    {
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Public Category")]
        public bool IsPublic { get; set; } = true;
    }

    // ViewModel for adding an emergency contact
    public class AddEmergencyContactViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
        public string Description { get; set; } = string.Empty;

        [Range(1, 10, ErrorMessage = "Priority must be between 1 and 10.")]
        public int Priority { get; set; } = 5;
    }

    // ViewModel for adding a vendor contact
    public class AddVendorContactViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Contact person name cannot exceed 100 characters.")]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service type is required.")]
        [StringLength(100, ErrorMessage = "Service type cannot exceed 100 characters.")]
        public string Service { get; set; } = string.Empty;

        [Url(ErrorMessage = "Invalid website URL.")]
        [StringLength(200, ErrorMessage = "Website URL cannot exceed 200 characters.")]
        public string Website { get; set; } = string.Empty;

        [Display(Name = "Preferred Vendor")]
        public bool IsPreferred { get; set; } = false;
    }

    public class AddDepartmentContactViewModel
    {
        [Required, StringLength(50)]
        public string DepartmentName { get; set; } = string.Empty;
        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
        [StringLength(50)]
        public string ContactPerson { get; set; } = string.Empty;
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        [StringLength(100)]
        public string OfficeHours { get; set; } = string.Empty;
        [StringLength(50)]
        public string Location { get; set; } = string.Empty;
    }

    public class AddCommunityContactViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [StringLength(200)]
        public string Bio { get; set; } = string.Empty;
        public bool ShowPhoneNumber { get; set; }
        public bool ShowEmail { get; set; }
        public bool ShowAddress { get; set; }
    }
}