using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface ICourseService
    {
        Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, Guid teacherId);
        Task<IEnumerable<CourseResponse>> GetTeacherCoursesAsync(Guid teacherId);
        Task<CourseResponse> GetCourseByIdAsync(Guid id);
        Task AssignClassesToCourseAsync(Guid courseId, List<Guid> classIds, Guid teacherId);
        Task<CourseProgressResponse> GetStudentCourseProgressAsync(Guid courseId, Guid studentId);
        Task<CertificateResponse> GetCertificateAsync(Guid certificateId);
        Task<IEnumerable<CertificateResponse>> GetStudentCertificatesAsync(Guid studentId);
        Task DeleteCourseAsync(Guid courseId, Guid teacherId);
    }
}
