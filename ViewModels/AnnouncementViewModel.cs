using GreenMeadowsPortal.Models; // Fix for ApplicationUser and Announcement
using System.Collections.Generic; // Fix for IEnumerable

namespace GreenMeadowsPortal.ViewModels
{
    public class AnnouncementViewModel
    {
        public ApplicationUser CurrentUser { get; set; } = default!; // Fix for CS0246 and CS8618
        public string FirstName { get; set; } = string.Empty; // Fix for CS8618
        public string Role { get; set; } = string.Empty; // Fix for CS8618
        public string ProfileImageUrl { get; set; } = string.Empty; // Fix for CS8618
        public int NotificationCount { get; set; } // Added this property to fix CS0117  

        public IEnumerable<Announcement> Announcements { get; set; } = new List<Announcement>(); // Fix for CS0246
    }
}
