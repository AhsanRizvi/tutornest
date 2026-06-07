using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public interface ITeacherService
    {
        // Class management
        Task<ClassResponse> CreateClassAsync(CreateClassRequest request, Guid teacherId);
        Task<IEnumerable<ClassResponse>> GetClassesAsync(Guid teacherId);
        Task<bool> EnrollStudentAsync(Guid classId, Guid studentId, Guid teacherId);
        Task<IEnumerable<StudentResponse>> GetClassStudentsAsync(Guid classId, Guid teacherId);

        // Student management
        Task<IEnumerable<StudentResponse>> GetMyStudentsAsync(Guid teacherId);

        // Video management
        Task<VideoResponse> CreateVideoAsync(CreateVideoRequest request, Guid teacherId);
        Task<IEnumerable<VideoResponse>> GetMyVideosAsync(Guid teacherId);
        Task<bool> AssignVideoToClassAsync(Guid classId, Guid videoId, Guid teacherId);
        Task<IEnumerable<VideoResponse>> GetClassVideosAsync(Guid classId, Guid teacherId);

        // Progress Overview
        Task<IEnumerable<StudentProgressReport>> GetStudentsProgressAsync(Guid teacherId);

        // Class updates and deletion
        Task<ClassResponse> UpdateClassAsync(Guid classId, CreateClassRequest request, Guid teacherId);
        Task DeleteClassAsync(Guid classId, Guid teacherId);

        // Student updates and deletion
        Task<StudentResponse> UpdateStudentAsync(Guid studentId, UpdateStudentRequest request, Guid teacherId);
        Task DeleteStudentAsync(Guid studentId, Guid teacherId);

        // Class-Student mapping deletion
        Task RemoveStudentFromClassAsync(Guid classId, Guid studentId, Guid teacherId);

        // Certificate management
        Task<CertificateResponse> AwardCertificateAsync(AwardCertificateRequest request, Guid teacherId);
        Task<IEnumerable<CertificateResponse>> GetTeacherCertificatesAsync(Guid teacherId);
        Task DeleteCertificateAsync(Guid certificateId, Guid teacherId);
    }
}
