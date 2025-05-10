using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.Services;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly NotificationService _notificationService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment hostEnvironment,
            NotificationService notificationService,
            ILogger<DocumentController> logger)
        {
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: /Document/
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");
            var isStaff = roles.Contains("Staff");

            // Create view model with user info for layout
            var viewModel = new DocumentLibraryViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                CurrentUserId = user.Id,
                CurrentUserRole = roles.FirstOrDefault() ?? "User",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id)
            };

            // Get appropriate documents based on role
            viewModel.DocumentCategories = GetDocumentCategories(isAdmin, isStaff);

            return View(viewModel);
        }

        // GET: /Document/Upload
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Upload()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);

            var viewModel = new DocumentUploadViewModel
            {
                FirstName = user.FirstName,
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png",
                Role = roles.FirstOrDefault() ?? "User",
                NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id),
                Categories = GetCategorySelectList()
            };

            return View(viewModel);
        }

        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (model.DocumentFile != null && model.DocumentFile.Length > 0)
                {
                    try
                    {
                        // Save document file
                        string documentUrl = await SaveDocumentFileAsync(model.DocumentFile, model.Category);
                        // Save visibility meta file
                        string subfolderPath = Path.Combine("documents", model.Category.ToLower().Replace(" ", "-"));
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, subfolderPath);
                        string fileName = Path.GetFileName(documentUrl);
                        string metaPath = Path.Combine(uploadsFolder, Path.ChangeExtension(fileName, ".meta"));
                        System.IO.File.WriteAllText(metaPath, model.VisibleTo);
                        TempData["SuccessMessage"] = "Document uploaded successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error uploading document: {ex.Message}");
                        ModelState.AddModelError(string.Empty, $"Error uploading document: {ex.Message}");
                    }
                }
                else
                {
                    ModelState.AddModelError("DocumentFile", "Please select a file to upload.");
                }
            }
            var roles = await _userManager.GetRolesAsync(user);
            model.FirstName = user.FirstName;
            model.ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-avatar.png";
            model.Role = roles.FirstOrDefault() ?? "User";
            model.NotificationCount = await _notificationService.GetUnreadCountAsync(user.Id);
            model.Categories = GetCategorySelectList();
            return View(model);
        }

        // GET: /Document/Download/5
        public IActionResult Download(string category, string fileName)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Category and file name must be provided");
            }

            // Get current user and roles synchronously
            var user = _userManager.GetUserAsync(User).GetAwaiter().GetResult();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var roles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
            var isAdmin = roles.Contains("Admin");
            var isStaff = roles.Contains("Staff");

            // Sanitize folder path to prevent path traversal attacks
            string safeCategory = Path.GetFileName(category);
            string safeFileName = Path.GetFileName(fileName);

            string folderPath = Path.Combine(_hostEnvironment.WebRootPath, "documents", safeCategory);
            string filePath = Path.Combine(folderPath, safeFileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // Check visibility
            string metaFile = Path.ChangeExtension(filePath, ".meta");
            string visibleTo = "All";
            if (System.IO.File.Exists(metaFile))
                visibleTo = System.IO.File.ReadAllText(metaFile).Trim();

            // Fix permission check logic
            if (visibleTo == "Admin" && !isAdmin) return Forbid();
            if (visibleTo == "Staff" && !(isAdmin || isStaff)) return Forbid();
            if (visibleTo == "Homeowners" && !(isAdmin || roles.Contains("Homeowner"))) return Forbid();

            return PhysicalFile(filePath, GetContentType(filePath), safeFileName);
        }

        // GET: /Document/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string category, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("Category and file name must be provided");
                }

                // Sanitize folder path to prevent path traversal attacks
                string safeCategory = Path.GetFileName(category);
                string safeFileName = Path.GetFileName(fileName);

                string folderPath = Path.Combine(_hostEnvironment.WebRootPath, "documents", safeCategory);
                string filePath = Path.Combine(folderPath, safeFileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound();

                // Get file metadata for the view
                var fileInfo = new FileInfo(filePath);
                var uploadDate = fileInfo.CreationTime;
                var fileSize = GetFileSize(fileInfo.Length);

                // Return view with document info
                var documentViewModel = new DocumentViewModel
                {
                    Name = Path.GetFileNameWithoutExtension(safeFileName),
                    FileType = Path.GetExtension(safeFileName).TrimStart('.').ToUpper(),
                    Category = safeCategory,
                    FileUrl = $"/documents/{safeCategory}/{safeFileName}",
                    UploadDate = uploadDate,
                    FileSize = fileSize,
                    Description = "Document details" // You might want to retrieve this from a database in a real app
                };

                return View(documentViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying delete confirmation");
                TempData["ErrorMessage"] = "Error displaying delete confirmation: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Document/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string category, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(fileName))
                {
                    return BadRequest("Category and file name must be provided");
                }

                // Sanitize folder path to prevent path traversal attacks
                string safeCategory = Path.GetFileName(category);
                string safeFileName = Path.GetFileName(fileName);

                _logger.LogInformation($"Attempting to delete file: {safeCategory}/{safeFileName}");

                string folderPath = Path.Combine(_hostEnvironment.WebRootPath, "documents", safeCategory);
                string filePath = Path.Combine(folderPath, safeFileName);
                string metaFile = Path.ChangeExtension(filePath, ".meta");

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation($"Deleted file: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"File not found for deletion: {filePath}");
                }

                if (System.IO.File.Exists(metaFile))
                {
                    System.IO.File.Delete(metaFile);
                    _logger.LogInformation($"Deleted meta file: {metaFile}");
                }

                TempData["SuccessMessage"] = "Document deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                TempData["ErrorMessage"] = "Error deleting document: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Helper methods
        private async Task<string> SaveDocumentFileAsync(IFormFile file, string category)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded");

            // Create category subfolder
            string subfolderPath = Path.Combine("documents", category.ToLower().Replace(" ", "-"));
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, subfolderPath);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            string fileName = Path.GetFileName(file.FileName);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/{subfolderPath}/{uniqueFileName}";
        }

        private List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> GetCategorySelectList()
        {
            return new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Community Guidelines", Text = "Community Guidelines" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Financial Reports", Text = "Financial Reports" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Meeting Minutes", Text = "Meeting Minutes" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Forms", Text = "Forms" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Newsletters", Text = "Newsletters" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Property Maps", Text = "Property Maps" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "Other", Text = "Other" }
            };
        }

        private List<DocumentCategoryViewModel> GetDocumentCategories(bool isAdmin, bool isStaff)
        {
            var categories = new List<DocumentCategoryViewModel>();
            var allCategories = new[]
            {
                new { Name = "Community Guidelines", Icon = "fa-book", Description = "Rules and guidelines for community residents", Folder = "community-guidelines" },
                new { Name = "Forms", Icon = "fa-file-alt", Description = "Downloadable forms for various community requests", Folder = "forms" },
                new { Name = "Meeting Minutes", Icon = "fa-clipboard", Description = "Minutes from community meetings and board meetings", Folder = "meeting-minutes" },
                new { Name = "Financial Reports", Icon = "fa-chart-pie", Description = "Financial statements and budgets", Folder = "financial-reports" },
                new { Name = "Newsletters", Icon = "fa-newspaper", Description = "Community newsletters and announcements", Folder = "newsletters" },
                new { Name = "Property Maps", Icon = "fa-map", Description = "Maps of the community and property layouts", Folder = "property-maps" },
                new { Name = "Administrative", Icon = "fa-user-shield", Description = "Administrative documents for management", Folder = "administrative" }
            };

            foreach (var cat in allCategories)
            {
                // Only show Financial Reports and Administrative to Admin/Staff
                if ((cat.Name == "Financial Reports" || cat.Name == "Administrative") && !(isAdmin || (isStaff && cat.Name == "Financial Reports")))
                    continue;

                categories.Add(new DocumentCategoryViewModel
                {
                    Name = cat.Name,
                    Icon = cat.Icon,
                    Description = cat.Description,
                    Documents = GetDocumentsByCategory(cat.Folder, isAdmin, isStaff)
                });
            }
            return categories;
        }

        // Dynamic document listing for all categories
        private List<DocumentViewModel> GetDocumentsByCategory(string folderName, bool isAdmin, bool isStaff)
        {
            var documents = new List<DocumentViewModel>();
            string folder = Path.Combine(_hostEnvironment.WebRootPath, "documents", folderName);
            if (!Directory.Exists(folder))
                return documents;

            var files = Directory.GetFiles(folder).Where(f => !f.EndsWith(".meta")).ToArray();
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                // Try to read metadata (VisibleTo) from a sidecar file, or default to All
                string metaFile = Path.ChangeExtension(filePath, ".meta");
                string visibleTo = "All";
                if (System.IO.File.Exists(metaFile))
                {
                    visibleTo = System.IO.File.ReadAllText(metaFile).Trim();
                }
                // Enforce visibility
                if (visibleTo == "Admin" && !isAdmin) continue;
                if (visibleTo == "Staff" && !(isAdmin || isStaff)) continue;
                // Fixed logic for Homeowners visibility
                var userRoles = User.IsInRole("Homeowner") ? new[] { "Homeowner" } : new string[0];
                if (visibleTo == "Homeowners" && !isAdmin && !(userRoles.Contains("Homeowner"))) continue;
                documents.Add(new DocumentViewModel
                {
                    Id = fileInfo.Name.GetHashCode(),
                    Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    Description = "Uploaded file",
                    FileType = fileInfo.Extension.TrimStart('.').ToUpper(),
                    FileSize = GetFileSize(fileInfo.Length),
                    VisibleTo = visibleTo,
                    Category = folderName,
                    UploadDate = fileInfo.CreationTime,
                    FileUrl = $"/documents/{folderName}/" + fileInfo.Name,
                    UploadedById = string.Empty,
                    UploadedByName = string.Empty
                });
            }
            return documents.OrderByDescending(d => d.UploadDate).ToList();
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream",
            };
        }

        private string GetFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}