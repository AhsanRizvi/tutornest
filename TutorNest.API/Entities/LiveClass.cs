using System;

namespace TutorNest.API.Entities
{
    public class LiveClass
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ScheduledStartTime { get; set; }
        public int DurationMinutes { get; set; }
        public string MeetingLink { get; set; } = string.Empty;
        public string? RecordingUrl { get; set; }

        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
