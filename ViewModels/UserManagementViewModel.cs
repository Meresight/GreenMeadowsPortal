namespace GreenMeadowsPortal.ViewModels
{
    public class UserManagementViewModel
    {
        // Admin/Current User Info
        public required string FirstName { get; set; }
        public required string Role { get; set; }
        public int NotificationCount { get; set; }
        public string CurrentUserProfileImageUrl { get; set; } = string.Empty;

        // User Listing Data
        public required List<UserViewModel> Users { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageStartRecord { get; set; }
        public int PageEndRecord { get; set; }
        public int TotalRecords { get; set; }

        // Pending Requests
        public required List<PendingUserViewModel> PendingUsers { get; set; } = new();
        public int PendingRequestsCount { get; set; }

        // Roles & Permissions
        public required List<RoleViewModel> Roles { get; set; } = new();

        // Activity Logs
        public required List<ActivityLogViewModel> ActivityLogs { get; set; } = new();
        public int LogsCurrentPage { get; set; }
        public int LogsTotalPages { get; set; }
        public int LogsPageStartRecord { get; set; }
        public int LogsPageEndRecord { get; set; }
        public int TotalLogs { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;

        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Address { get; set; }
        public required string Role { get; set; }
        public required string Status { get; set; }
        public required string MemberSince { get; set; }
        public required string LastLogin { get; set; }
        public required string ProfileImageUrl { get; set; }
        public required string PropertyType { get; set; }
        public required string OwnershipStatus { get; set; }
        public bool SendCredentials { get; set; } // Added property
        public string? Password { get; set; }


    }

    public class PendingUserViewModel : UserViewModel
    {
        public required List<DocumentViewModel> Documents { get; set; } = new();
    }

    public class RoleViewModel
    {
        public required string Name { get; set; }
        public int UserCount { get; set; }
        public required List<PermissionViewModel> Permissions { get; set; } = new();
    }

    public class PermissionViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Category { get; set; }
        public bool IsGranted { get; set; }
    }

    public class ActivityLogViewModel
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string UserRole { get; set; }
        public required string UserProfileImage { get; set; }
        public required string ActivityType { get; set; }
        public required string Details { get; set; }
        public required string IpAddress { get; set; }
        public required string Timestamp { get; set; }
    }
}
