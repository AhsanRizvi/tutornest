using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(Guid userId, string message, string type);
        Task<IEnumerable<Notification>> GetNotificationsAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
    }
}
