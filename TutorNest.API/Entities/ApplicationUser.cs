using Microsoft.AspNetCore.Identity;

namespace TutorNest.API.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string? Bio { get; set; }
        public string? Subject { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public bool IsSuspended { get; set; } = false;
        public string? ReferralCode { get; set; }
        public Guid? ReferredById { get; set; }
        public ApplicationUser? ReferredBy { get; set; }

        // Relationships
        public ICollection<Class> CreatedClasses { get; set; } = new List<Class>();
        public ICollection<Video> UploadedVideos { get; set; } = new List<Video>();
        public ICollection<ApplicationUser> ReferredTeachers { get; set; } = new List<ApplicationUser>();
        
        // Many-to-many navigation properties
        public ICollection<TeacherStudent> TeacherStudents { get; set; } = new List<TeacherStudent>();
        public ICollection<ClassStudent> EnrolledClasses { get; set; } = new List<ClassStudent>();
        public ICollection<VideoProgress> VideoProgresses { get; set; } = new List<VideoProgress>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
