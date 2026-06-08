using System;

namespace TutorNest.API.DTOs
{
    public record SubscriptionPlanResponse(
        Guid Id,
        string Name,
        decimal Price,
        string Currency,
        int ClassLimit,
        int StudentLimit,
        long StorageLimitBytes,
        bool IsActive
    );

    public record TeacherSubscriptionResponse(
        Guid Id,
        Guid PlanId,
        string PlanName,
        decimal Price,
        string Currency,
        string Status,
        DateTime StartDate,
        DateTime EndDate,
        long StorageUsedBytes,
        long StorageLimitBytes,
        int ClassCount,
        int ClassLimit,
        int StudentCount,
        int StudentLimit
    );

    public record PaymentHistoryResponse(
        Guid Id,
        string PlanName,
        decimal Amount,
        string Currency,
        string Status,
        string PaymentProvider,
        string TransactionId,
        DateTime PaymentDate
    );

    public record CheckoutRequest(
        Guid PlanId,
        string SuccessUrl,
        string CancelUrl
    );

    public record CheckoutResponse(
        string SessionUrl,
        bool Success
    );

    public record ProfileUpdateRequest(
        string FirstName,
        string LastName,
        string? Bio,
        string? Subject,
        string? ProfilePictureUrl
    );

    public record UserProfileResponse(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        string? Bio,
        string? Subject,
        string? ProfilePictureUrl,
        string Role,
        string? ReferralCode = null,
        System.Collections.Generic.IEnumerable<string>? ReferredTutors = null
    );

    public record TeacherDetailsResponse(
        Guid Id,
        string Email,
        string FirstName,
        string LastName,
        TeacherSubscriptionResponse Subscription,
        bool IsSuspended,
        string Theme
    );

    public record CreatePlanRequest(
        string Name,
        decimal Price,
        string Currency,
        int ClassLimit,
        int StudentLimit,
        long StorageLimitBytes,
        bool IsActive
    );

    public record AdminRevenueReportResponse(
        decimal TotalRevenue,
        int ActiveSubscriptionsCount,
        IEnumerable<PaymentHistoryResponse> Transactions
    );

    public record UpdateThemeRequest(string Theme);
}
