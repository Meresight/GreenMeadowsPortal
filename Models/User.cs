// Models/User.cs
// This new model can be used for dashboard display purposes without conflicting with Identity

using System;

namespace GreenMeadowsPortal.Models
{
    // Rename to DashboardUser to avoid conflicts with Identity
    public class DashboardUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }

        // Add any other properties needed for UI display
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}