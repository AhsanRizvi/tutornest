namespace TutorNest.API.DTOs
{
    public record StudentResponse(
        Guid Id, 
        string Email, 
        string FirstName, 
        string LastName
    );

    public record StudentProgressReport(
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        Guid VideoId,
        string VideoTitle,
        double WatchTimeSeconds,
        double DurationSeconds,
        bool IsCompleted,
        DateTime LastWatchedAt
    );

    public record StudentVideoResponse(
        Guid Id,
        string Title,
        string Description,
        string VideoUrl,
        DateTime CreatedAt,
        double WatchTimeSeconds,
        double DurationSeconds,
        bool IsCompleted,
        DateTime? LastWatchedAt
    );
}
