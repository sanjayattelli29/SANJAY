using Application.Interfaces.Infrastructure;
using Domain.Entities;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Infrastructure.Broadcasters
{
    public class SignalRNotificationBroadcaster : INotificationBroadcaster
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task BroadcastNotificationAsync(string userId, Notification notification)
        {
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
