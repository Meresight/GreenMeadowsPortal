namespace GreenMeadowsPortal.ViewModels
{
    public class DocumentLibraryViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
        public List<DocumentCategoryViewModel> DocumentCategories { get; set; } = new List<DocumentCategoryViewModel>();
    }
}