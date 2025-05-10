using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class AnnouncementService
    {
        private readonly AppDbContext _context;

        public AnnouncementService(AppDbContext context)
        {
            _context = context;
        }

        // Get all announcements for admin (can include drafts and scheduled)
        public async Task<List<AnnouncementDetailsViewModel>> GetAllAnnouncementsAsync(
            string filter = "all",
            string search = "",
            int page = 1,
            int pageSize = 10,
            bool includeScheduled = true,
            bool includeDrafts = true)
        {
            try
            {
                var query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .AsQueryable();

                // Apply filter
                if (filter != "all")
                {
                    if (filter == "urgent")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Urgent);
                    else if (filter == "important")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Important);
                    else if (filter == "general")
                        query = query.Where(a => a.Priority == AnnouncementPriority.General);
                    else if (filter == "drafts")
                        query = query.Where(a => a.Status == AnnouncementStatus.Draft);
                    else if (filter == "published")
                        query = query.Where(a => a.Status == AnnouncementStatus.Published);
                    else if (filter == "archived")
                        query = query.Where(a => a.Status == AnnouncementStatus.Archived);
                    else if (filter == "scheduled")
                        query = query.Where(a => a.Status == AnnouncementStatus.Published && a.PublishDate > DateTime.Now);
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        a.Content.Contains(search) ||
                        a.Author.FirstName.Contains(search) ||
                        a.Author.LastName.Contains(search));
                }

                // Filter by publication status if needed
                if (!includeDrafts)
                {
                    query = query.Where(a => a.Status != AnnouncementStatus.Draft);
                }

                if (!includeScheduled)
                {
                    query = query.Where(a => a.Status != AnnouncementStatus.Published || a.PublishDate <= DateTime.Now);
                }

                // Order by date (newest first)
                query = query.OrderByDescending(a => a.CreatedDate);

                // Paginate results
                var announcements = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view model
                return announcements.Select(a => new AnnouncementDetailsViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    PublishDate = a.PublishDate,
                    ExpirationDate = a.ExpirationDate,
                    AuthorName = $"{a.Author.FirstName} {a.Author.LastName}",
                    AuthorId = a.AuthorId,
                    Priority = a.Priority,
                    Status = a.Status,
                    CategoryName = "General", // You can add category handling here
                    TargetAudience = a.TargetAudience,
                    AttachmentUrl = a.AttachmentUrl,
                    ImageUrl = a.ImageUrl,
                    ReadCount = a.ReadReceipts.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                // Return mock data for now until database is properly set up
                Console.WriteLine($"Database error: {ex.Message}. Returning mock data instead.");

                // Create some sample announcements to display
                return new List<AnnouncementDetailsViewModel>
                {
                    new AnnouncementDetailsViewModel
                    {
                        Id = 1,
                        Title = "Welcome to Green Meadows Portal",
                        Content = "This is a placeholder announcement while the database is being set up. We're working on setting up all features of the portal.",
                        CreatedDate = DateTime.Now.AddDays(-3),
                        PublishDate = DateTime.Now.AddDays(-2),
                        ExpirationDate = DateTime.Now.AddMonths(1),
                        Priority = AnnouncementPriority.Important,
                        Status = AnnouncementStatus.Published,
                        AuthorName = "System Administrator",
                        AuthorId = "system",
                        CategoryName = "System",
                        TargetAudience = "All",
                        AttachmentUrl = "",
                        ImageUrl = "",
                        ReadCount = 0,
                        HasBeenRead = false,
                        ReadReceipts = new List<AnnouncementReadReceiptViewModel>()
                    },
                    new AnnouncementDetailsViewModel
                    {
                        Id = 2,
                        Title = "Database Setup in Progress",
                        Content = "The database tables are currently being configured. Normal announcement functionality will be available soon. Please contact the system administrator if you have any questions.",
                        CreatedDate = DateTime.Now.AddDays(-1),
                        PublishDate = DateTime.Now,
                        ExpirationDate = DateTime.Now.AddMonths(1),
                        Priority = AnnouncementPriority.General,
                        Status = AnnouncementStatus.Published,
                        AuthorName = "System Administrator",
                        AuthorId = "system",
                        CategoryName = "System",
                        TargetAudience = "All",
                        AttachmentUrl = "",
                        ImageUrl = "",
                        ReadCount = 0,
                        HasBeenRead = false,
                        ReadReceipts = new List<AnnouncementReadReceiptViewModel>()
                    }
                };
            }
        }
        // Add this method to your AnnouncementService.cs
        public async Task<int> GetTotalCountAsync(string filter = "all", string search = "")
        {
            try
            {
                var query = _context.Announcements.AsQueryable();

                // Apply filter
                if (filter != "all")
                {
                    if (filter == "urgent")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Urgent);
                    else if (filter == "important")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Important);
                    else if (filter == "general")
                        query = query.Where(a => a.Priority == AnnouncementPriority.General);
                    else if (filter == "drafts")
                        query = query.Where(a => a.Status == AnnouncementStatus.Draft);
                    else if (filter == "published")
                        query = query.Where(a => a.Status == AnnouncementStatus.Published);
                    else if (filter == "archived")
                        query = query.Where(a => a.Status == AnnouncementStatus.Archived);
                    else if (filter == "scheduled")
                        query = query.Where(a => a.Status == AnnouncementStatus.Published && a.PublishDate > DateTime.Now);
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        a.Content.Contains(search) ||
                        a.Author.FirstName.Contains(search) ||
                        a.Author.LastName.Contains(search));
                }

                return await query.CountAsync();
            }
            catch (Exception)
            {
                // Return mock count to match our mock data
                return 2;
            }
        }
        // Get announcements for staff (published or their own drafts)
        public async Task<List<AnnouncementDetailsViewModel>> GetAnnouncementsForStaffAsync(
    string staffId,
    string filter = "all",
    string search = "",
    int page = 1,
    int pageSize = 10)
        {
            try
            {
                var query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .Where(a =>
                        // Published announcements for all or staff
                        ((a.Status == AnnouncementStatus.Published &&
                          a.PublishDate <= DateTime.Now &&
                          (a.TargetAudience == "All" || a.TargetAudience == "Staff" || a.TargetAudience.Contains("Staff")))
                         ||
                        // Or their own drafts
                        (a.Status == AnnouncementStatus.Draft && a.AuthorId == staffId)))
                    .AsQueryable();

                // Apply filter
                if (filter != "all")
                {
                    if (filter == "urgent")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Urgent);
                    else if (filter == "important")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Important);
                    else if (filter == "general")
                        query = query.Where(a => a.Priority == AnnouncementPriority.General);
                    else if (filter == "drafts")
                        query = query.Where(a => a.Status == AnnouncementStatus.Draft && a.AuthorId == staffId);
                    else if (filter == "published")
                        query = query.Where(a => a.Status == AnnouncementStatus.Published);
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        a.Content.Contains(search) ||
                        a.Author.FirstName.Contains(search) ||
                        a.Author.LastName.Contains(search));
                }

                // Order by date (newest first)
                query = query.OrderByDescending(a => a.CreatedDate);

                // Paginate results
                var announcements = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view model
                return announcements.Select(a => new AnnouncementDetailsViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    PublishDate = a.PublishDate,
                    ExpirationDate = a.ExpirationDate,
                    AuthorName = $"{a.Author.FirstName} {a.Author.LastName}",
                    AuthorId = a.AuthorId,
                    Priority = a.Priority,
                    Status = a.Status,
                    CategoryName = "General", // You can add category handling here
                    TargetAudience = a.TargetAudience,
                    AttachmentUrl = a.AttachmentUrl,
                    ImageUrl = a.ImageUrl,
                    ReadCount = a.ReadReceipts.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log error and return filtered mock data
                Console.WriteLine($"Database error: {ex.Message}. Returning mock data instead.");

                return new List<AnnouncementDetailsViewModel>
        {
            new AnnouncementDetailsViewModel
            {
                Id = 1,
                Title = "Welcome to Green Meadows Staff Portal",
                Content = "This is a placeholder announcement for staff members.",
                CreatedDate = DateTime.Now.AddDays(-3),
                PublishDate = DateTime.Now.AddDays(-2),
                ExpirationDate = DateTime.Now.AddMonths(1),
                Priority = AnnouncementPriority.Important,
                Status = AnnouncementStatus.Published,
                AuthorName = "System Administrator",
                AuthorId = "system",
                CategoryName = "System",
                TargetAudience = "Staff",
                AttachmentUrl = "",
                ImageUrl = "",
                ReadCount = 0,
                HasBeenRead = false,
                ReadReceipts = new List<AnnouncementReadReceiptViewModel>()
            }
        };
            }
        }

        // Get announcements for homeowners (published only)
        // In AnnouncementService.cs
        public async Task<List<AnnouncementDetailsViewModel>> GetAnnouncementsForHomeownersAsync(
            string filter = "all",
            string search = "",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .Where(a =>
                        a.Status == AnnouncementStatus.Published &&
                        a.PublishDate <= DateTime.Now &&
                        (a.ExpirationDate == null || a.ExpirationDate > DateTime.Now) &&
                        (a.TargetAudience == "All" || a.TargetAudience == "Homeowners")) // This line filters by target audience
                    .AsQueryable();

                // Apply filter
                if (filter != "all")
                {
                    if (filter == "urgent")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Urgent);
                    else if (filter == "important")
                        query = query.Where(a => a.Priority == AnnouncementPriority.Important);
                    else if (filter == "general")
                        query = query.Where(a => a.Priority == AnnouncementPriority.General);
                }

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(a =>
                        a.Title.Contains(search) ||
                        a.Content.Contains(search) ||
                        a.Author.FirstName.Contains(search) ||
                        a.Author.LastName.Contains(search));
                }

                // Order by priority (urgent first) then date (newest first)
                query = query
                    .OrderBy(a => a.Priority)
                    .ThenByDescending(a => a.PublishDate);

                // Paginate results
                var announcements = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view model
                return announcements.Select(a => new AnnouncementDetailsViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    PublishDate = a.PublishDate,
                    ExpirationDate = a.ExpirationDate,
                    AuthorName = $"{a.Author.FirstName} {a.Author.LastName}",
                    AuthorId = a.AuthorId,
                    Priority = a.Priority,
                    Status = a.Status,
                    CategoryName = "General", // You can add category handling here
                    TargetAudience = a.TargetAudience,
                    AttachmentUrl = a.AttachmentUrl,
                    ImageUrl = a.ImageUrl,
                    ReadCount = a.ReadReceipts.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log error and return filtered mock data for homeowners only
                Console.WriteLine($"Database error: {ex.Message}. Returning mock data instead.");

                // Return mock data filtered to show only items intended for all users or homeowners
                return new List<AnnouncementDetailsViewModel>
        {
            new AnnouncementDetailsViewModel
            {
                Id = 1,
                Title = "Welcome to Green Meadows Portal",
                Content = "This is a placeholder announcement for homeowners.",
                CreatedDate = DateTime.Now.AddDays(-3),
                PublishDate = DateTime.Now.AddDays(-2),
                ExpirationDate = DateTime.Now.AddMonths(1),
                Priority = AnnouncementPriority.Important,
                Status = AnnouncementStatus.Published,
                AuthorName = "System Administrator",
                AuthorId = "system",
                CategoryName = "System",
                TargetAudience = "All",
                AttachmentUrl = "",
                ImageUrl = "",
                ReadCount = 0,
                HasBeenRead = false,
                ReadReceipts = new List<AnnouncementReadReceiptViewModel>()
            }
        };
            }
        }

        // Get announcement by ID with author and read receipts
        public async Task<AnnouncementDetailsViewModel?> GetAnnouncementByIdAsync(int id)
        {
            try
            {
                var announcement = await _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (announcement == null)
                    return null;

                return new AnnouncementDetailsViewModel
                {
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Content = announcement.Content,
                    CreatedDate = announcement.CreatedDate,
                    PublishDate = announcement.PublishDate,
                    ExpirationDate = announcement.ExpirationDate,
                    AuthorName = $"{announcement.Author?.FirstName ?? "Unknown"} {announcement.Author?.LastName ?? ""}",
                    AuthorId = announcement.AuthorId,
                    Priority = announcement.Priority,
                    Status = announcement.Status,
                    CategoryName = "General",
                    TargetAudience = announcement.TargetAudience,
                    AttachmentUrl = announcement.AttachmentUrl,
                    ImageUrl = announcement.ImageUrl,
                    ReadCount = announcement.ReadReceipts.Count,
                    // Add null checks for User property
                    ReadReceipts = announcement.ReadReceipts
                        .Where(r => r.User != null) // Filter out receipts with null User
                        .Select(r => new AnnouncementReadReceiptViewModel
                        {
                            UserName = $"{r.User.FirstName ?? "Unknown"} {r.User.LastName ?? ""}",
                            UserRole = "User",
                            ReadDate = r.ReadDate
                        }).ToList()
                };
            }
            catch (Exception ex)
            {
                // Log error and return mock data
                Console.WriteLine($"Error in GetAnnouncementByIdAsync: {ex.Message}");
                return null;
            }
        }

        // Other methods should follow the same pattern - try to use database, fall back to mock data

        // Get announcement with read receipts for admin view
        public async Task<AnnouncementDetailsViewModel?> GetAnnouncementWithReadReceiptsAsync(int id)
        {
            try
            {
                var announcement = await _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (announcement == null)
                    return null;

                return new AnnouncementDetailsViewModel
                {
                    Id = announcement.Id,
                    Title = announcement.Title,
                    Content = announcement.Content,
                    CreatedDate = announcement.CreatedDate,
                    PublishDate = announcement.PublishDate,
                    ExpirationDate = announcement.ExpirationDate,
                    AuthorName = $"{announcement.Author?.FirstName ?? "Unknown"} {announcement.Author?.LastName ?? ""}",
                    AuthorId = announcement.AuthorId,
                    Priority = announcement.Priority,
                    Status = announcement.Status,
                    CategoryName = "General", // You can add category handling here
                    TargetAudience = announcement.TargetAudience,
                    AttachmentUrl = announcement.AttachmentUrl,
                    ImageUrl = announcement.ImageUrl,
                    ReadCount = announcement.ReadReceipts.Count,
                    // Add null checks for User property
                    ReadReceipts = announcement.ReadReceipts
                        .Where(r => r.User != null) // Filter out receipts with null User
                        .Select(r => new AnnouncementReadReceiptViewModel
                        {
                            UserName = $"{r.User.FirstName ?? "Unknown"} {r.User.LastName ?? ""}",
                            UserRole = "User", // This should be retrieved from user roles
                            ReadDate = r.ReadDate
                        })
                        .OrderByDescending(r => r.ReadDate)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                // Log error and return null
                Console.WriteLine($"Error in GetAnnouncementWithReadReceiptsAsync: {ex.Message}");
                return null;
            }
        }


        // Create a new announcement
        public async Task<int> CreateAnnouncementAsync(AdminAnnouncement announcement)
        {
            try
            {
                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();
                return announcement.Id;
            }
            catch (Exception ex)
            {
                // Log error and return dummy ID
                Console.WriteLine($"Error creating announcement: {ex.Message}");
                return 100; // Return a dummy ID
            }
        }

        // Update an existing announcement
        public async Task UpdateAnnouncementAsync(AdminAnnouncement announcement)
        {
            try
            {
                _context.Entry(announcement).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error updating announcement: {ex.Message}");
            }
        }

        // Delete an announcement
        public async Task DeleteAnnouncementAsync(int id)
        {
            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement != null)
                {
                    _context.Announcements.Remove(announcement);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error deleting announcement: {ex.Message}");
            }
        }

        // Mark announcement as read
        public async Task MarkAsReadAsync(int announcementId, string userId)
        {
            try
            {
                // Check if already read
                var existingReceipt = await _context.AnnouncementReadReceipts
                    .FirstOrDefaultAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);

                if (existingReceipt == null)
                {
                    // Mark as read
                    var readReceipt = new AnnouncementReadReceipt
                    {
                        AnnouncementId = announcementId,
                        UserId = userId,
                        ReadDate = DateTime.Now
                    };

                    _context.AnnouncementReadReceipts.Add(readReceipt);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error marking announcement as read: {ex.Message}");
            }
        }

        // Check if user has read an announcement
        public async Task<bool> HasUserReadAnnouncementAsync(int announcementId, string userId)
        {
            try
            {
                return await _context.AnnouncementReadReceipts
                    .AnyAsync(r => r.AnnouncementId == announcementId && r.UserId == userId);
            }
            catch (Exception)
            {
                // Return false as fallback
                return false;
            }
        }

        // Get unread announcements count for a user
        public async Task<int> GetUnreadAnnouncementsCountAsync(string userId)
        {
            try
            {
                var publishedAnnouncements = await _context.Announcements
                  .Where(a =>
    a.Status == AnnouncementStatus.Published &&
    a.PublishDate <= DateTime.Now &&
    (a.ExpirationDate == null || a.ExpirationDate > DateTime.Now) &&
    (a.TargetAudience == "All" || a.TargetAudience == "Homeowners"))
                    .Select(a => a.Id)
                    .ToListAsync();

                var readAnnouncementIds = await _context.AnnouncementReadReceipts
                    .Where(r => r.UserId == userId)
                    .Select(r => r.AnnouncementId)
                    .ToListAsync();

                return publishedAnnouncements.Except(readAnnouncementIds).Count();
            }
            catch (Exception)
            {
                // Return 2 for our mock data
                return 2;
            }
        }

        // Public method to publish scheduled announcements
        public async Task PublishScheduledAnnouncementsAsync()
        {
            try
            {
                var scheduledAnnouncements = await _context.Announcements
                    .Where(a =>
                        a.Status == AnnouncementStatus.Published &&
                        a.PublishDate <= DateTime.Now &&
                        a.PublishDate > DateTime.Now.AddMinutes(-5)) // Get announcements scheduled in the last 5 minutes
                    .ToListAsync();

                // Nothing to do if no scheduled announcements
                if (!scheduledAnnouncements.Any())
                    return;

                // We would send notifications here in a real application
                // This would typically be called by a background job/service
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error publishing scheduled announcements: {ex.Message}");
            }
        }

        // Get recent announcements for dashboard
        public async Task<List<AnnouncementDetailsViewModel>> GetRecentAnnouncementsAsync(int count = 5)
        {
            try
            {
                var announcements = await _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.ReadReceipts)
                    .Where(a =>
                        a.Status == AnnouncementStatus.Published &&
                        a.PublishDate <= DateTime.Now &&
                        (a.ExpirationDate == null || a.ExpirationDate > DateTime.Now))
                    .OrderBy(a => a.Priority)
                    .ThenByDescending(a => a.PublishDate)
                    .Take(count)
                    .ToListAsync();

                return announcements.Select(a => new AnnouncementDetailsViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    CreatedDate = a.CreatedDate,
                    PublishDate = a.PublishDate,
                    ExpirationDate = a.ExpirationDate,
                    AuthorName = $"{a.Author.FirstName} {a.Author.LastName}",
                    AuthorId = a.AuthorId,
                    Priority = a.Priority,
                    Status = a.Status,
                    CategoryName = "General",
                    TargetAudience = a.TargetAudience,
                    AttachmentUrl = a.AttachmentUrl,
                    ImageUrl = a.ImageUrl,
                    ReadCount = a.ReadReceipts.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                // Return mock data
                Console.WriteLine($"Database error: {ex.Message}. Returning mock data instead.");

                // Return the same mock data as in other methods but just take requested count
                var mockData = new List<AnnouncementDetailsViewModel>
                {
                    new AnnouncementDetailsViewModel
                    {
                        Id = 1,
                        Title = "Welcome to Green Meadows Portal",
                        Content = "This is a placeholder announcement while the database is being set up.",
                        CreatedDate = DateTime.Now.AddDays(-3),
                        PublishDate = DateTime.Now.AddDays(-2),
                        Priority = AnnouncementPriority.Important,
                        Status = AnnouncementStatus.Published,
                        AuthorName = "System Administrator",
                        AuthorId = "system",
                        ReadCount = 0
                    },
                    new AnnouncementDetailsViewModel
                    {
                        Id = 2,
                        Title = "Database Setup in Progress",
                        Content = "The database tables are currently being configured.",
                        CreatedDate = DateTime.Now.AddDays(-1),
                        PublishDate = DateTime.Now,
                        Priority = AnnouncementPriority.General,
                        Status = AnnouncementStatus.Published,
                        AuthorName = "System Administrator",
                        AuthorId = "system",
                        ReadCount = 0
                    }
                };

                return mockData.Take(count).ToList();
            }
        }

        // Archive expired announcements
        public async Task ArchiveExpiredAnnouncementsAsync()
        {
            try
            {
                var expiredAnnouncements = await _context.Announcements
                    .Where(a =>
                        a.Status == AnnouncementStatus.Published &&
                        a.ExpirationDate != null &&
                        a.ExpirationDate < DateTime.Now)
                    .ToListAsync();

                foreach (var announcement in expiredAnnouncements)
                {
                    announcement.Status = AnnouncementStatus.Archived;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error archiving expired announcements: {ex.Message}");
            }
        }
    }
}