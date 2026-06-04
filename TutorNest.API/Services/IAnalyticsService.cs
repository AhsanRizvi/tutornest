using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IAnalyticsService
    {
        Task<TeacherAnalyticsResponse> GetTeacherAnalyticsAsync(Guid teacherId);
        Task<AdminAnalyticsResponse> GetAdminAnalyticsAsync();
    }
}
