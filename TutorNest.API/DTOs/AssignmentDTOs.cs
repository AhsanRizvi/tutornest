namespace TutorNest.API.DTOs
{
    public record CreateAssignmentRequest(
        string Title,
        string Description,
        DateTime DueDate,
        double TotalMarks,
        Guid ClassId,
        string Type, // "MultipleChoice", "ShortAnswer", "FileUpload"
        string? ConfigJson
    );

    public record AssignmentResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime DueDate,
        double TotalMarks,
        string Type,
        string? ConfigJson,
        Guid ClassId,
        bool? IsSubmitted, // Contextual for students
        double? ScoreEarned, // Contextual for students
        bool? IsGraded, // Contextual for students
        string? Feedback // Contextual for students
    );

    public record SubmitAssignmentRequest(
        string? AnswerText,
        string? AttachmentUrl
    );

    public record SubmissionResponse(
        Guid Id,
        Guid AssignmentId,
        string AssignmentTitle,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        string? AnswerText,
        string? AttachmentUrl,
        double? Grade,
        string? Feedback,
        DateTime SubmittedAt,
        DateTime? GradedAt
    );

    public record GradeSubmissionRequest(
        double Grade,
        string Feedback
    );
}
