namespace TutorNest.API.Entities
{
    public class AssignmentSubmission
    {
        public Guid Id { get; set; }

        public Guid AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public Guid StudentId { get; set; }
        public ApplicationUser Student { get; set; } = null!;

        public string? AnswerText { get; set; }
        public string? AttachmentUrl { get; set; }

        public double? Grade { get; set; }
        public string? Feedback { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? GradedAt { get; set; }
    }
}
