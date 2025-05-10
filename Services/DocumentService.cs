using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class DocumentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            AppDbContext context,
            IWebHostEnvironment hostEnvironment,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task<List<DocumentModel>> GetAllDocumentsAsync()
        {
            return await _context.Documents
                .Include(d => d.UploadedBy)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<List<DocumentModel>> GetDocumentsByCategoryAsync(string category)
        {
            return await _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.Category == category)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<List<DocumentModel>> GetDocumentsForUserAsync(string userRole)
        {
            // Filter documents based on user role
            IQueryable<DocumentModel> query = _context.Documents
                .Include(d => d.UploadedBy);

            if (userRole == "Admin")
            {
                // Admin can see all documents
                return await query.OrderByDescending(d => d.UploadDate).ToListAsync();
            }
            else if (userRole == "Staff")
            {
                // Staff can see documents visible to All, Staff, and Homeowners
                query = query.Where(d => d.VisibleTo == "All" || d.VisibleTo == "Staff" || d.VisibleTo == "Homeowners");
            }
            else
            {
                // Homeowners can only see documents visible to All and Homeowners
                query = query.Where(d => d.VisibleTo == "All" || d.VisibleTo == "Homeowners");
            }

            return await query.OrderByDescending(d => d.UploadDate).ToListAsync();
        }

        public async Task<DocumentModel> GetDocumentByIdAsync(int id)
        {
            return await _context.Documents
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DocumentModel> CreateDocumentAsync(DocumentModel document, IFormFile file)
        {
            // Save file to disk
            string fileUrl = await SaveDocumentFileAsync(file, document.Category);
            string fileType = Path.GetExtension(file.FileName).TrimStart('.').ToUpper();
            string fileSize = GetFileSize(file.Length);

            // Set file properties
            document.FileUrl = fileUrl;
            document.FileType = fileType;
            document.FileSize = fileSize;
            document.UploadDate = DateTime.Now;

            // Add to database
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return document;
        }

        public async Task UpdateDocumentAsync(DocumentModel document)
        {
            _context.Entry(document).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return;

            // Delete physical file
            DeleteDocumentFile(document.FileUrl);

            // Remove from database
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
        }

        public async Task<string> SaveDocumentFileAsync(IFormFile file, string category)
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

        public void DeleteDocumentFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            string filePath = Path.Combine(_hostEnvironment.WebRootPath, fileUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error deleting file {filePath}: {ex.Message}");
                }
            }
        }

        public string GetFileSize(long bytes)
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