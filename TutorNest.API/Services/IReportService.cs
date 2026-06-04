using System;
using System.Threading.Tasks;

namespace TutorNest.API.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateClassProgressReportAsync(Guid classId, Guid teacherId);
        Task<byte[]> GenerateAssignmentResultsReportAsync(Guid assignmentId, Guid teacherId);
        Task<byte[]> GenerateAdminPlatformReportAsync();
        Task<byte[]> GenerateCertificatePdfAsync(Guid certificateId);
        Task<byte[]> GenerateAdminRevenueReportAsync();
    }
}
