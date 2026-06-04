namespace TutorNest.API.Entities
{
    public class TeacherStudent
    {
        public Guid TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; } = null!;

        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;
    }
}
