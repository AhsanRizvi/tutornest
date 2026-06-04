namespace TutorNest.API.Entities
{
    public class Video
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty; // local file path, absolute URL, etc.

        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ClassVideo> AssignedClasses { get; set; } = new List<ClassVideo>();
        public ICollection<VideoProgress> Progresses { get; set; } = new List<VideoProgress>();
    }
}
