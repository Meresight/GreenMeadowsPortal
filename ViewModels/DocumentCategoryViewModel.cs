namespace GreenMeadowsPortal.ViewModels
{
    public class DocumentCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DocumentViewModel> Documents { get; set; } = new List<DocumentViewModel>();
    }
}
