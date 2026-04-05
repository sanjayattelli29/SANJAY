using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

// this handles real-time notification delivery to users
namespace Infrastructure.Hubs
{
    /// <summary>
    /// This is the SignalR "Hub" for notifications.
    /// Think of it as a live "Telephone Operator" that keeps a connection open
    /// between the server and every logged-in user's browser.
    /// </summary>
    [Authorize]  // only logged in users can connect
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This method automatically runs whenever a user's browser connects to the hub.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Identify who just connected (their User ID and Name).
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name;
            
            // Log this connection so we know the user is "Live" and ready to receive alerts.
            _logger.LogInformation("NotificationHub connected: UserID={UserId}, Name={UserName}, ConnectionId={ConnectionId}", 
                userId, userName, Context.ConnectionId);
            
            // Finish the connection setup process.
            await base.OnConnectedAsync();
        }
    }
}