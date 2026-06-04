namespace TutorNest.API.Entities
{
    public class Assignment
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public double TotalMarks { get; set; }
        
        // Type: "MultipleChoice", "ShortAnswer", "FileUpload"
        public string Type { get; set; } = string.Empty;

        // ConfigJson: JSON string containing options and answers if MCQ. E.g. {"options": ["A", "B"], "correctAnswer": "A"}
        public string? ConfigJson { get; set; }

        public Guid ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}
