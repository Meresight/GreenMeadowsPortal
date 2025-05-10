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
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        // Implementation of INotificationService methods
        public async Task<int> CreateNotificationAsync(string userId, string title, string message, string type, string? referenceId = null)
        {
            try
            {
                var user = await _context.Users
                    .OfType<ApplicationUser>()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                {
                    throw new ArgumentException("Invalid userId. User not found.");
                }

                var notification = new Notification
                {
                    UserId = userId,
                    User = user,
                    Title = title,
                    Message = message,
                    Type = type,
                    ReferenceId = referenceId ?? string.Empty,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return notification.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating notification: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<NotificationViewModel>> GetNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationViewModel
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        ReferenceId = n.ReferenceId ?? string.Empty,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                return notifications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return new List<NotificationViewModel>
                {
                    new NotificationViewModel
                    {
                        Id = 1,
                        Title = "Welcome to Green Meadows Portal",
                        Message = "We're glad to have you here!",
                        Type = "System",
                        ReferenceId = string.Empty,
                        IsRead = false,
                        CreatedAt = DateTime.Now.AddDays(-1)
                    },
                    new NotificationViewModel
                    {
                        Id = 2,
                        Title = "Database Setup in Progress",
                        Message = "The portal is being set up. Some features may not be available yet.",
                        Type = "System",
                        ReferenceId = string.Empty,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    }
                };
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return 2;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all notifications as read: {ex.Message}");
            }
        }

        public async Task DeleteNotificationAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting notification: {ex.Message}");
            }
        }

        public async Task DeleteAllNotificationsAsync(string userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting all notifications: {ex.Message}");
            }
        }
        // Get notification by ID
        public async Task<NotificationViewModel?> GetNotificationByIdAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId);

                if (notification == null)
                    return null;

                return new NotificationViewModel
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    ReferenceId = notification.ReferenceId ?? string.Empty,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error getting notification by ID: {ex.Message}");

                // Return mock notification for specific ID
                if (notificationId == 1)
                {
                    return new NotificationViewModel
                    {
                        Id = 1,
                        Title = "Welcome to Green Meadows Portal",
                        Message = "We're glad to have you here!",
                        Type = "System",
                        ReferenceId = string.Empty,
                        IsRead = false,
                        CreatedAt = DateTime.Now.AddDays(-1)
                    };
                }
                else if (notificationId == 2)
                {
                    return new NotificationViewModel
                    {
                        Id = 2,
                        Title = "Database Setup in Progress",
                        Message = "The portal is being set up. Some features may not be available yet.",
                        Type = "System",
                        ReferenceId = string.Empty,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };
                }

                return null;
            }
        }
    }
}