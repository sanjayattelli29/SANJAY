using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface INotificationBroadcaster
    {
        Task BroadcastNotificationAsync(string userId, Notification notification);
    }
}
