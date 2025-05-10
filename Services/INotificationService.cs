// Services/INotificationService.cs
using GreenMeadowsPortal.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenMeadowsPortal.Services
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync(string userId);
        Task<int> CreateNotificationAsync(string userId, string title, string message, string type, string? referenceId = null);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId);
        Task DeleteAllNotificationsAsync(string userId);
        Task<List<NotificationViewModel>> GetNotificationsForUserAsync(string userId, int page = 1, int pageSize = 10);
    }
}