using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    /// <summary>
    /// This service handles the entire "Alert and Inbox" system.
    /// It saves notifications to the database and also broadcasts them in real-time.
    /// </summary>
    public class SystemNotifier : ISystemNotifier
    {
        // Tools for saving to DB and sending live alerts to the browser.
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationBroadcaster _broadcaster;

        public SystemNotifier(
            INotificationRepository notificationRepository, 
            INotificationBroadcaster broadcaster)
        {
            _notificationRepository = notificationRepository;
            _broadcaster = broadcaster;
        }

        /// <summary>
        /// This method sends a new notification to a user.
        /// It saves it in their "Inbox" (Database) and also pops up on their screen (Broadcast).
        /// </summary>
        public async Task SendNotificationAsync(string userId, string title, string message, string type = "General")
        {
            // 1. Create the notification record.
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            // 2. Save it to the database so the user can see it later.
            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            // 3. Send it immediately to the browser so the user sees a popup right now.
            await _broadcaster.BroadcastNotificationAsync(userId, notification);
        }

        // Get the last 50 notifications for a user's inbox list.
        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetByUserIdAsync(userId, 50);
        }

        // Count how many items in the inbox are still marked as "Unread".
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        // Mark one specific alert as "Seen".
        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _notificationRepository.SaveChangesAsync();
            }
        }

        // Mark every single alert in the inbox as "Seen".
        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            await _notificationRepository.SaveChangesAsync();
        }

        // Delete a single alert permanently.
        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }

        // Clear out the entire notification history for a user.
        public async Task DeleteAllNotificationsAsync(string userId)
        {
            await _notificationRepository.DeleteAllAsync(userId);
            await _notificationRepository.SaveChangesAsync();
        }
    }
}
