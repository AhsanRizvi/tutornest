using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly TutorNestDbContext _context;

        public NotificationService(TutorNestDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(Guid userId, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to top 50 notifications
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any()) return true;

            foreach (var n in notifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
