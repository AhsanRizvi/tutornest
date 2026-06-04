using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface ILiveClassService
    {
        Task<LiveClassResponse> ScheduleLiveClassAsync(CreateLiveClassRequest request, Guid teacherId);
        Task<IEnumerable<LiveClassResponse>> GetClassLiveClassesAsync(Guid classId);
        Task<IEnumerable<LiveClassResponse>> GetTeacherUpcomingLiveClassesAsync(Guid teacherId);
        Task<IEnumerable<LiveClassResponse>> GetStudentUpcomingLiveClassesAsync(Guid studentId);
        Task SaveRecordingUrlAsync(Guid liveClassId, string recordingUrl, Guid teacherId);
    }
}
