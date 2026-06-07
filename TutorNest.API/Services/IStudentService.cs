using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<ClassResponse>> GetMyClassesAsync(Guid studentId);
        Task<IEnumerable<StudentVideoResponse>> GetClassVideosAsync(Guid classId, Guid studentId);
        Task<ProgressResponse> UpdateProgressAsync(Guid videoId, Guid studentId, UpdateProgressRequest request);
        Task<IEnumerable<LeaderboardEntry>> GetClassLeaderboardAsync(Guid classId, Guid studentId);
    }
}
