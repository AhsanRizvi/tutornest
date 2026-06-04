namespace TutorNest.API.Entities
{
    public class Class
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public Guid? CourseId { get; set; }
        public Course? Course { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<ClassStudent> EnrolledStudents { get; set; } = new List<ClassStudent>();
        public ICollection<ClassVideo> AssignedVideos { get; set; } = new List<ClassVideo>();
        public ICollection<LiveClass> LiveClasses { get; set; } = new List<LiveClass>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
