using Domain.Entities;

namespace Application.Interfaces
{
    public interface ISystemNotifier
    {
        Task SendNotificationAsync(string userId, string title, string message, string type = "General");
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(Guid notificationId);
        Task DeleteAllNotificationsAsync(string userId);
    }
}
