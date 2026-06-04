using System;

namespace TutorNest.API.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g. Free, Basic, Pro
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public int ClassLimit { get; set; }
        public int StudentLimit { get; set; }
        public long StorageLimitBytes { get; set; } // storage size constraint in bytes
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
