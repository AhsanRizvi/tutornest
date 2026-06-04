using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IAdminService
    {
        Task<IEnumerable<TeacherDetailsResponse>> GetTeachersAsync();
        Task SuspendUserAsync(Guid userId);
        Task UnsuspendUserAsync(Guid userId);
        Task<SubscriptionPlanResponse> CreatePlanAsync(CreatePlanRequest request);
        Task<SubscriptionPlanResponse> UpdatePlanAsync(Guid planId, CreatePlanRequest request);
        Task<AdminRevenueReportResponse> GetRevenueReportAsync();
    }
}
