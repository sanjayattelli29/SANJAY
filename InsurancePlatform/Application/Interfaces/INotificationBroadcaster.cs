using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface is used to send "Real-Time" alerts to users.
    /// For example, showing a popup message immediately when a claim is approved.
    /// </summary>
    public interface INotificationBroadcaster
    {
        // Sends a specific notification to a user's browser or app right now.
        Task BroadcastNotificationAsync(string userId, Notification notification);
    }
}
