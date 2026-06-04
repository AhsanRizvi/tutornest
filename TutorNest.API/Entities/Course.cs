using System;
using System.Collections.Generic;

namespace TutorNest.API.Entities
{
    public class Course
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Class> ClassGroups { get; set; } = new List<Class>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
