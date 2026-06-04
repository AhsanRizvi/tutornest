namespace TutorNest.API.Entities
{
    public class ClassStudent
    {
        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}
