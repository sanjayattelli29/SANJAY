using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces.Infrastructure
{
    public interface INotificationBroadcaster
    {
        Task BroadcastNotificationAsync(string userId, Notification notification);
    }
}
