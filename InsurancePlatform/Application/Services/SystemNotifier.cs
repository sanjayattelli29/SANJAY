using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Application.Services
{
    public class SystemNotifier : ISystemNotifier
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationBroadcaster _broadcaster;

        public SystemNotifier(
            INotificationRepository notificationRepository, 
            INotificationBroadcaster broadcaster)
        {
            _notificationRepository = notificationRepository;
            _broadcaster = broadcaster;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string type = "General")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = type,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            await _broadcaster.BroadcastNotificationAsync(userId, notification);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetByUserIdAsync(userId, 50);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification);
                await _notificationRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteAllNotificationsAsync(string userId)
        {
            await _notificationRepository.DeleteAllAsync(userId);
            await _notificationRepository.SaveChangesAsync();
        }
    }
}
