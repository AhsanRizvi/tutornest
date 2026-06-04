namespace TutorNest.API.Entities
{
    public class ClassVideo
    {
        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid VideoId { get; set; }
        public Video Video { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
