using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface ISubscriptionService
    {
        Task<IEnumerable<SubscriptionPlanResponse>> GetActivePlansAsync();
        Task<TeacherSubscriptionResponse> GetTeacherSubscriptionAsync(Guid teacherId);
        Task<bool> IsWithinClassLimitAsync(Guid teacherId);
        Task<bool> IsWithinStudentLimitAsync(Guid teacherId);
        Task<bool> IsWithinStorageLimitAsync(Guid teacherId, long additionalBytes);
        Task TrackFileUploadAsync(Guid teacherId, Guid uploadedById, string fileName, string filePath, long fileSizeBytes);
        Task TrackFileDeletionAsync(Guid teacherId, string filePath);
        Task UpgradeSubscriptionAsync(Guid teacherId, Guid planId, string provider, string transactionId, string? externalSubId = null);
        Task<IEnumerable<PaymentHistoryResponse>> GetPaymentHistoryAsync(Guid teacherId);
        Task<UserProfileResponse> GetUserProfileAsync(Guid userId);
        Task UpdateUserProfileAsync(Guid userId, ProfileUpdateRequest request);
        Task<Guid> GetTeacherIdForUserAsync(Guid userId, string role);
    }
}
