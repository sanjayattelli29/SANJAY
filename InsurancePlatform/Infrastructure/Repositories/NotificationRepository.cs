using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// This class manages all the "Storage and Lookup" for Notifications (Alerts).
    /// It helps show users their recent messages and track which ones are unread.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor sets up the database connection.
        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Find a single notification alert using its unique ID.
        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        // Get the most recent notifications for a user (up to 50 by default).
        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int limit = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        // Count how many notifications the user has NOT opened yet.
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        // Get a list of all "Unread" notifications for a specific user.
        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
        }

        // Save a brand new notification alert to the database.
        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        // Remove a specific notification from the database.
        public async Task DeleteAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await Task.CompletedTask;
        }

        // Clear all notifications for a specific user (Delete All).
        public async Task DeleteAllAsync(string userId)
        {
            var userNotifications = _context.Notifications.Where(n => n.UserId == userId);
            _context.Notifications.RemoveRange(userNotifications);
            await Task.CompletedTask;
        }

        // Finalize and save all changes to the SQL Server database.
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
