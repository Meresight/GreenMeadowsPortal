namespace GreenMeadowsPortal.ViewModels
{
    public class DocumentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string VisibleTo { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string UploadedById { get; set; } = string.Empty;
        public string UploadedByName { get; set; } = string.Empty;
    }
}