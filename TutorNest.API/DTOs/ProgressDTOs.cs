namespace TutorNest.API.DTOs
{
    public record UpdateProgressRequest(
        double WatchTimeSeconds, 
        double DurationSeconds, 
        bool IsCompleted
    );

    public record ProgressResponse(
        Guid VideoId, 
        double WatchTimeSeconds, 
        double DurationSeconds, 
        bool IsCompleted, 
        DateTime LastWatchedAt
    );
}
