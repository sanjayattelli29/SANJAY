using Domain.Entities;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface is the main "Notification Central".
    /// It is used to send alerts to users and manage their notification inbox.
    /// </summary>
    public interface ISystemNotifier
    {
        // Create and send a brand new notification to a specific user
        Task SendNotificationAsync(string userId, string title, string message, string type = "General");
        
        // Get all notifications (the whole inbox) for a specific user
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
        
        // Count how many notifications are still marked as 'New' or 'Unseen'
        Task<int> GetUnreadCountAsync(string userId);
        
        // Mark one specific notification as 'Seen'
        Task MarkAsReadAsync(Guid notificationId);
        
        // Mark every single notification in the user's inbox as 'Seen'
        Task MarkAllAsReadAsync(string userId);
        
        // Delete a single notification permanently
        Task DeleteNotificationAsync(Guid notificationId);
        
        // Clear out the entire notification inbox for a user
        Task DeleteAllNotificationsAsync(string userId);
    }
}
