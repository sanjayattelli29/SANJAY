using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

// this handles real-time notification delivery to users
namespace Infrastructure.Hubs
{
    [Authorize]  // only logged in users can connect
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        // this runs when a user connects to the notification system
        public override async Task OnConnectedAsync()
        {
            // get user info from their login token
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name;
            // write to log file that they connected
            _logger.LogInformation("NotificationHub connected: UserID={UserId}, Name={UserName}, ConnectionId={ConnectionId}", 
                userId, userName, Context.ConnectionId);
            
            // complete the connection setup
            await base.OnConnectedAsync();
        }
    }
}