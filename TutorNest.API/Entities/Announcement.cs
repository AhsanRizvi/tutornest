namespace TutorNest.API.Entities
{
    public class Announcement
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }

        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        // If ClassId is null, this announcement is for all classes/students under this teacher
        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
