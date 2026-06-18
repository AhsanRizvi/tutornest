using System;

namespace TutorNest.API.Entities
{
    public class Certificate
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public Guid? CourseId { get; set; }
        public Course? Course { get; set; }

        public Guid? ClassId { get; set; }
        public Class? Class { get; set; }

        public string CertificateCode { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        // Custom template fields editable by teachers
        public string? CustomTitle { get; set; }
        public string? CustomSubTitle { get; set; }
        public string? CustomMessage { get; set; }
        public string? LogoUrl { get; set; }
    }
}
