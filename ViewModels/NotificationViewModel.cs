using System;
using System.Collections.Generic;

namespace GreenMeadowsPortal.ViewModels
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public required string Title { get; set; } // Added 'required' modifier
        public required string Message { get; set; } // Added 'required' modifier
        public required string Type { get; set; } // Added 'required' modifier
        public required string ReferenceId { get; set; } // Added 'required' modifier
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class NotificationsListViewModel
    {
        public required string FirstName { get; set; } // Added 'required' modifier
        public required string Role { get; set; } // Added 'required' modifier
        public required string ProfileImageUrl { get; set; } // Added 'required' modifier
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
        public int UnreadCount { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class NotificationPreferencesViewModel
    {
        public required string FirstName { get; set; } // Added 'required' modifier
        public required string Role { get; set; } // Added 'required' modifier
        public required string ProfileImageUrl { get; set; } // Added 'required' modifier
        public bool ReceiveEmailNotifications { get; set; }
        public bool ReceiveSmsNotifications { get; set; }
        public bool NotifyForAnnouncements { get; set; } = true;
        public bool NotifyForServiceRequests { get; set; } = true;
        public bool NotifyForBillingUpdates { get; set; } = true;
        public bool NotifyForEvents { get; set; } = true;
        public bool NotifyForForumActivity { get; set; } = true;
    }
}
