using System;

namespace TutorNest.API.Entities
{
    public class PaymentHistory
    {
        public Guid Id { get; set; }

        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public Guid SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public string Status { get; set; } = "Paid"; // Paid, Failed, Pending
        public string PaymentProvider { get; set; } = string.Empty; // Stripe, PayHere, Mock
        public string TransactionId { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}
