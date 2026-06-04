namespace TutorNest.API.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        public string Message { get; set; } = string.Empty;
        
        // Type: "Assignment", "Announcement", "Grade"
        public string Type { get; set; } = string.Empty;
        
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
