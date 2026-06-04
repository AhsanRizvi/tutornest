using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IAnnouncementService
    {
        Task<AnnouncementResponse> CreateAnnouncementAsync(CreateAnnouncementRequest request, Guid teacherId);
        Task<IEnumerable<AnnouncementResponse>> GetStudentAnnouncementsAsync(Guid studentId);
        Task<IEnumerable<AnnouncementResponse>> GetTeacherAnnouncementsAsync(Guid teacherId);
        Task<bool> MarkAsReadAsync(Guid announcementId, Guid studentId);
    }
}
