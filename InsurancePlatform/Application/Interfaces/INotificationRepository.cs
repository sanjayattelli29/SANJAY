using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// This interface defines how we store and find old notifications in the database.
    /// It helps users see their notification history (like an inbox).
    /// </summary>
    public interface INotificationRepository
    {
        // Find a specific notification using its unique ID
        Task<Notification?> GetByIdAsync(Guid id);
        
        // Get a list of recent notifications for a specific user
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int limit = 50);
        
        // Count how many notifications the user hasn't clicked on yet
        Task<int> GetUnreadCountAsync(string userId);
        
        // Get the actual list of notifications that are still marked as 'Unread'
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId);
        
        // Save a new notification into the database
        Task AddAsync(Notification notification);
        
        // Remove a specific notification
        Task DeleteAsync(Notification notification);
        
        // Clear all notifications for a specific user
        Task DeleteAllAsync(string userId);
        
        // Save all changes to the database permanently
        Task SaveChangesAsync();
    }
}
