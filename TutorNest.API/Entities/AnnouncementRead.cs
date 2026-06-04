namespace TutorNest.API.Entities
{
    public class AnnouncementRead
    {
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid AnnouncementId { get; set; }
        public Announcement Announcement { get; set; } = null!;

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
