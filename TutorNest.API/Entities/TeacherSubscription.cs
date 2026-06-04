using System;

namespace TutorNest.API.Entities
{
    public class TeacherSubscription
    {
        public Guid Id { get; set; }
        
        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public string Status { get; set; } = "Active"; // Active, Expired, Canceled
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }

        public long StorageUsedBytes { get; set; } = 0;

        public string? PaymentProvider { get; set; } // Stripe, PayHere, Mock, Admin
        public string? ExternalSubscriptionId { get; set; } // Stripe/PayHere identifier

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
