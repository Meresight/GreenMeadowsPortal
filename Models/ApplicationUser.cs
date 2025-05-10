using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace GreenMeadowsPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Personal Information  
        [PersonalData]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty; // Fix: Initialize with a default value  

        [PersonalData]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty; // Fix: Initialize with a default value  

        [PersonalData]
        public DateTime? DateOfBirth { get; set; }

        [PersonalData]
        public string ProfileImageUrl { get; set; } = string.Empty; // Fix: Initialize with a default value  

        // Contact & Address Information  
        [PersonalData]
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty; // Fix: Initialize with a default value  

        [PersonalData]
        [MaxLength(20)]
        public string Unit { get; set; } = string.Empty; // Fix: Initialize with a default value  

        // Account & Portal Settings  
        [PersonalData]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive, Suspended, Pending, Rejected  

        [PersonalData]
        public DateTime MemberSince { get; set; } = DateTime.Now;

        [PersonalData]
        public DateTime? LastLoginDate { get; set; }

        [PersonalData]
        public bool ForcePasswordChange { get; set; } = true;

        [PersonalData]
        public bool? ReceiveEmailNotifications { get; set; } = true;

        [PersonalData]
        public bool? ReceiveSmsNotifications { get; set; } = false;

        // Homeowner Specific Fields  
        [PersonalData]
        [MaxLength(50)]
        public string PropertyType { get; set; } = string.Empty; // Apartment, Townhouse, Single Family Home, Villa  

        [PersonalData]
        [MaxLength(20)]
        public string? OwnershipStatus { get; set; } // Owner, Tenant  

        [PersonalData]
        public DateTime? MoveInDate { get; set; }

        [PersonalData]
        public int? ResidentCount { get; set; }

        [PersonalData]
        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [PersonalData]
        [MaxLength(20)]
        public string? EmergencyContactPhone { get; set; }

        [PersonalData]
        [MaxLength(200)]
        public string VehicleInfo { get; set; } = string.Empty; // Fix: Initialize with a default value  

        // Staff Specific Fields  
        [PersonalData]
        [MaxLength(50)]
        public string Department { get; set; } = string.Empty; // Management, Maintenance, Security, Landscaping, Administrative  

        [PersonalData]
        [MaxLength(50)]
        public string Position { get; set; } = string.Empty; // Fix: Initialize with a default value  

        [PersonalData]
        [MaxLength(20)]
        public string EmployeeId { get; set; } = string.Empty; // Fix: Initialize with a default value  

        // Additional Information  
        [PersonalData]
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty; // Fix: Initialize with a default value  

        // Utility Properties  
        public string FullName => $"{FirstName} {LastName}";
    }
}