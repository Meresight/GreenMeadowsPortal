using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GreenMeadowsPortal.ViewModels
{
    public class StaffProfileViewModel
    {
        // Basic profile information
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public IFormFile? ProfileImage { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NotificationCount { get; set; }

        // Emergency contacts
        public string PrimaryContactName { get; set; } = string.Empty;
        public string PrimaryContactRelationship { get; set; } = string.Empty;
        public string PrimaryContactPhone { get; set; } = string.Empty;
        public string PrimaryContactEmail { get; set; } = string.Empty;

        public string SecondaryContactName { get; set; } = string.Empty;
        public string SecondaryContactRelationship { get; set; } = string.Empty;
        public string SecondaryContactPhone { get; set; } = string.Empty;
        public string SecondaryContactEmail { get; set; } = string.Empty;

        // Skills
        public List<StaffSkill> Skills { get; set; } = new List<StaffSkill>();

        // Work schedule
        public WorkSchedule Schedule { get; set; } = new WorkSchedule();
    }

    public class StaffSkill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public DateTime AcquiredDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class WorkSchedule
    {
        public string Monday { get; set; } = string.Empty;
        public string Tuesday { get; set; } = string.Empty;
        public string Wednesday { get; set; } = string.Empty;
        public string Thursday { get; set; } = string.Empty;
        public string Friday { get; set; } = string.Empty;
        public string Saturday { get; set; } = string.Empty;
        public string Sunday { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
