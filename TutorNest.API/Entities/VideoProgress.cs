namespace TutorNest.API.Entities
{
    public class VideoProgress
    {
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid VideoId { get; set; }
        public Video Video { get; set; } = null!;

        public double WatchTimeSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
    }
}
