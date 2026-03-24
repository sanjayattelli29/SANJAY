using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(Guid id);
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int limit = 50);
        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId);
        Task AddAsync(Notification notification);
        Task DeleteAsync(Notification notification);
        Task DeleteAllAsync(string userId);
        Task SaveChangesAsync();
    }
}
