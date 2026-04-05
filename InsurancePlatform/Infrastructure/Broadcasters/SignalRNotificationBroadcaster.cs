using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Infrastructure.Broadcasters
{
    /// <summary>
    /// This class is responsible for "Broadcasting" or "Pushing" notifications to users in real-time.
    /// It uses SignalR so that the user sees the alert immediately without refreshing the page.
    /// </summary>
    public class SignalRNotificationBroadcaster : INotificationBroadcaster
    {
        // This is the SignalR "Hub Context" that allows us to talk to connected browsers.
        private readonly IHubContext<NotificationHub> _hubContext;

        // The constructor gets the SignalR hub ready for use.
        public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// This method sends a notification package to a specific user's browser.
        /// </summary>
        public async Task BroadcastNotificationAsync(string userId, Notification notification)
        {
            // We find the specific user by their ID and send them the "ReceiveNotification" message.
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                userId = notification.UserId,
                title = notification.Title,
                message = notification.Message,
                createdAt = notification.CreatedAt,
                isRead = notification.IsRead,
                notificationType = notification.NotificationType
            });
        }
    }
}
