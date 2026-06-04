using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(LoginRequest request);
        Task<ApplicationUser?> RegisterTeacherAsync(RegisterTeacherRequest request);
        Task<ApplicationUser?> RegisterStudentAsync(RegisterRequest request, Guid teacherId);
    }
}
