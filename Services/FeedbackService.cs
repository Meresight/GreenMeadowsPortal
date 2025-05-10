using GreenMeadowsPortal.Data;
using GreenMeadowsPortal.Models;
using GreenMeadowsPortal.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public class FeedbackService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            ILogger<FeedbackService> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        // Create feedback
        public async Task<int> CreateFeedbackAsync(FeedbackCreateViewModel model, string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError($"User {userId} not found when creating feedback");
                    return 0;
                }

                var feedback = new Feedback
                {
                    UserId = userId,
                    Type = model.Type,
                    Subject = model.Subject,
                    Description = model.Description,
                    Category = model.Category,
                    Status = FeedbackStatus.New,
                    SubmittedDate = DateTime.Now,
                    Priority = DeterminePriority(model.Type, model.Description)
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                // Reload feedback with User included
                var savedFeedback = await _context.Feedbacks.Include(f => f.User).FirstOrDefaultAsync(f => f.Id == feedback.Id);
                if (savedFeedback != null)
                {
                    feedback.User = savedFeedback.User;
                }

                // Notify admins about new feedback
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    await _notificationService.CreateNotificationAsync(
                        admin.Id,
                        "New Feedback",
                        $"New feedback received: {model.Subject}",
                        "Feedback",
                        feedback.Id.ToString()
                    );
                }

                _logger.LogInformation($"Feedback {feedback.Id} created by user {userId}");
                return feedback.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback");
                return 0;
            }
        }

        // Get feedback by ID with details
        public async Task<FeedbackDetailsViewModel?> GetFeedbackDetailsAsync(int id, string currentUserId)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.AdminUser)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (feedback == null) return null;

                // Increment view count
                feedback.ViewCount++;
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                var roles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                return new FeedbackDetailsViewModel
                {
                    Id = feedback.Id,
                    Type = feedback.Type,
                    Subject = feedback.Subject,
                    Description = feedback.Description,
                    Status = feedback.Status,
                    SubmittedDate = feedback.SubmittedDate,
                    ResolvedDate = feedback.ResolvedDate,
                    Priority = feedback.Priority,
                    Category = feedback.Category,
                    ViewCount = feedback.ViewCount,
                    UserId = feedback.UserId,
                    SubmittedBy = $"{feedback.User.FirstName} {feedback.User.LastName}",
                    SubmittedByAvatar = feedback.User.ProfileImageUrl ?? "/images/default-avatar.png",
                    SubmittedByEmail = feedback.User.Email ?? string.Empty,
                    AdminResponse = feedback.AdminResponse,
                    ResolvedBy = feedback.AdminUser != null ? $"{feedback.AdminUser.FirstName} {feedback.AdminUser.LastName}" : null,
                    ResolvedByAvatar = feedback.AdminUser?.ProfileImageUrl ?? "/images/default-avatar.png",
                    CanEdit = feedback.UserId == currentUserId && feedback.Status == FeedbackStatus.New,
                    CanDelete = isAdmin || (feedback.UserId == currentUserId && feedback.Status == FeedbackStatus.New),
                    CanRespond = isAdmin || isStaff,
                    IsOwnSubmission = feedback.UserId == currentUserId,
                    FirstName = currentUser?.FirstName ?? string.Empty,
                    ProfileImageUrl = currentUser?.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = roles.FirstOrDefault() ?? "Homeowner",
                    NotificationCount = currentUser != null ? await _notificationService.GetUnreadCountAsync(currentUserId) : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting feedback details for ID {id}");
                return null;
            }
        }

        // Get feedback list with filtering
        public async Task<FeedbackListViewModel> GetFeedbackListAsync(
            string currentUserId,
            string statusFilter = "all",
            string typeFilter = "all",
            string searchQuery = "",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                var roles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var query = _context.Feedbacks
                    .Include(f => f.User)
                    .Include(f => f.AdminUser)
                    .AsQueryable();

                // Apply role-based filtering
                if (!isAdmin && !isStaff)
                {
                    query = query.Where(f => f.UserId == currentUserId);
                }

                // Apply status filter
                if (statusFilter != "all")
                {
                    if (Enum.TryParse<FeedbackStatus>(statusFilter, out var status))
                    {
                        query = query.Where(f => f.Status == status);
                    }
                }

                // Apply type filter
                if (typeFilter != "all")
                {
                    query = query.Where(f => f.Type == typeFilter);
                }

                // Apply search
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(f =>
                        f.Subject.Contains(searchQuery) ||
                        f.Description.Contains(searchQuery) ||
                        f.User.FirstName.Contains(searchQuery) ||
                        f.User.LastName.Contains(searchQuery));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var feedbacks = await query
                    .OrderByDescending(f => f.SubmittedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view models
                var feedbackItems = feedbacks.Select(f => new FeedbackItemViewModel
                {
                    Id = f.Id,
                    Type = f.Type,
                    Subject = f.Subject,
                    Description = f.Description,
                    Status = f.Status,
                    SubmittedDate = f.SubmittedDate,
                    ResolvedDate = f.ResolvedDate,
                    SubmittedBy = $"{f.User.FirstName} {f.User.LastName}",
                    SubmittedByAvatar = f.User.ProfileImageUrl ?? "/images/default-avatar.png",
                    AdminResponse = f.AdminResponse,
                    ResolvedBy = f.AdminUser != null ? $"{f.AdminUser.FirstName} {f.AdminUser.LastName}" : null,
                    Priority = f.Priority,
                    Category = f.Category,
                    ViewCount = f.ViewCount,
                    CanEdit = f.UserId == currentUserId && f.Status == FeedbackStatus.New,
                    CanDelete = isAdmin || (f.UserId == currentUserId && f.Status == FeedbackStatus.New),
                    CanRespond = isAdmin || isStaff
                }).ToList();

                // Get statistics
                var allFeedbacks = await _context.Feedbacks
                    .Where(f => isAdmin || isStaff || f.UserId == currentUserId)
                    .ToListAsync();

                return new FeedbackListViewModel
                {
                    FirstName = currentUser?.FirstName ?? string.Empty,
                    ProfileImageUrl = currentUser?.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = roles.FirstOrDefault() ?? "Homeowner",
                    CurrentUserId = currentUserId,
                    NotificationCount = currentUser != null ? await _notificationService.GetUnreadCountAsync(currentUserId) : 0,
                    Submissions = feedbackItems,
                    StatusFilter = statusFilter,
                    TypeFilter = typeFilter,
                    SearchQuery = searchQuery,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    TotalCount = totalCount,
                    NewCount = allFeedbacks.Count(f => f.Status == FeedbackStatus.New),
                    ResolvedCount = allFeedbacks.Count(f => f.Status == FeedbackStatus.Resolved),
                    TotalSubmissions = allFeedbacks.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feedback list");
                return new FeedbackListViewModel();
            }
        }

        // Resolve feedback
        public async Task<bool> ResolveFeedbackAsync(int id, string adminUserId, string adminResponse)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null) return false;

                feedback.Status = FeedbackStatus.Resolved;
                feedback.ResolvedDate = DateTime.Now;
                feedback.AdminResponse = adminResponse;
                feedback.AdminUserId = adminUserId;

                await _context.SaveChangesAsync();

                // Notify the user
                await _notificationService.CreateNotificationAsync(
                    feedback.UserId,
                    "Feedback Resolved",
                    $"Your feedback '{feedback.Subject}' has been resolved.",
                    "Feedback",
                    id.ToString()
                );

                _logger.LogInformation($"Feedback {id} resolved by admin {adminUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving feedback {id}");
                return false;
            }
        }

        // Delete feedback
        public async Task<bool> DeleteFeedbackAsync(int id, string currentUserId)
        {
            try
            {
                var feedback = await _context.Feedbacks.FindAsync(id);
                if (feedback == null) return false;

                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                var roles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
                var isAdmin = roles.Contains("Admin");

                // Check permissions
                if (!isAdmin && feedback.UserId != currentUserId)
                {
                    return false;
                }

                // Only allow deletion if status is New
                if (!isAdmin && feedback.Status != FeedbackStatus.New)
                {
                    return false;
                }

                _context.Feedbacks.Remove(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Feedback {id} deleted by user {currentUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting feedback {id}");
                return false;
            }
        }

        // Get dashboard statistics
        public async Task<FeedbackDashboardViewModel> GetDashboardStatisticsAsync(string currentUserId)
        {
            try
            {
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                var roles = currentUser != null ? await _userManager.GetRolesAsync(currentUser) : new List<string>();
                var isAdmin = roles.Contains("Admin");
                var isStaff = roles.Contains("Staff");

                var feedbacks = await _context.Feedbacks
                    .Include(f => f.User)
                    .Where(f => isAdmin || isStaff || f.UserId == currentUserId)
                    .ToListAsync();

                // Calculate statistics
                var newCount = feedbacks.Count(f => f.Status == FeedbackStatus.New);
                var resolvedCount = feedbacks.Count(f => f.Status == FeedbackStatus.Resolved);
                var complaintsCount = feedbacks.Count(f => f.Type == "Complaint");
                var suggestionsCount = feedbacks.Count(f => f.Type == "Suggestion");

                // Calculate average resolution time
                var resolvedFeedbacks = feedbacks.Where(f => f.Status == FeedbackStatus.Resolved && f.ResolvedDate.HasValue);
                var avgResolutionTime = resolvedFeedbacks.Any()
                    ? resolvedFeedbacks.Average(f => (f.ResolvedDate!.Value - f.SubmittedDate).TotalHours)
                    : 0;

                // Get recent submissions
                var recentSubmissions = feedbacks
                    .OrderByDescending(f => f.SubmittedDate)
                    .Take(5)
                    .Select(f => new FeedbackItemViewModel
                    {
                        Id = f.Id,
                        Type = f.Type,
                        Subject = f.Subject,
                        Description = f.Description,
                        Status = f.Status,
                        SubmittedDate = f.SubmittedDate,
                        ResolvedDate = f.ResolvedDate,
                        SubmittedBy = $"{f.User.FirstName} {f.User.LastName}",
                        SubmittedByAvatar = f.User.ProfileImageUrl ?? "/images/default-avatar.png",
                        Priority = f.Priority,
                        Category = f.Category
                    })
                    .ToList();

                // Group data for charts
                var feedbackByType = feedbacks
                    .GroupBy(f => f.Type)
                    .ToDictionary(g => g.Key, g => g.Count());

                var feedbackByCategory = feedbacks
                    .Where(f => !string.IsNullOrEmpty(f.Category))
                    .GroupBy(f => f.Category!)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Last 30 days trend
                var feedbackTrend = feedbacks
                    .Where(f => f.SubmittedDate >= DateTime.Now.AddDays(-30))
                    .GroupBy(f => f.SubmittedDate.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                return new FeedbackDashboardViewModel
                {
                    FirstName = currentUser?.FirstName ?? string.Empty,
                    ProfileImageUrl = currentUser?.ProfileImageUrl ?? "/images/default-avatar.png",
                    Role = roles.FirstOrDefault() ?? "Homeowner",
                    NotificationCount = await _notificationService.GetUnreadCountAsync(currentUserId),
                    NewFeedbackCount = newCount,
                    ResolvedFeedbackCount = resolvedCount,
                    TotalFeedbackCount = feedbacks.Count,
                    ComplaintsCount = complaintsCount,
                    SuggestionsCount = suggestionsCount,
                    AverageResolutionTime = avgResolutionTime,
                    SatisfactionRate = resolvedCount > 0 ? (resolvedCount / (double)feedbacks.Count) * 100 : 0,
                    RecentSubmissions = recentSubmissions,
                    FeedbackByType = feedbackByType,
                    FeedbackByCategory = feedbackByCategory,
                    FeedbackTrend = feedbackTrend
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return new FeedbackDashboardViewModel();
            }
        }

        // Helper methods
        private FeedbackPriority DeterminePriority(string type, string description)
        {
            // Default to Normal priority
            var priority = FeedbackPriority.Normal;

            // Check for urgent keywords in description
            var urgentKeywords = new[] { "urgent", "emergency", "critical", "immediate", "asap" };
            if (urgentKeywords.Any(k => description.ToLower().Contains(k)))
            {
                return FeedbackPriority.Urgent;
            }

            // Set priority based on type
            switch (type.ToLower())
            {
                case "complaint":
                    priority = FeedbackPriority.High;
                    break;
                case "suggestion":
                    priority = FeedbackPriority.Low;
                    break;
                case "general":
                    priority = FeedbackPriority.Normal;
                    break;
            }

            return priority;
        }
    }
}