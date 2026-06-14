namespace TutorNest.API.DTOs
{
    // Teacher Analytics
    public record TeacherAnalyticsResponse(
        IEnumerable<ClassProgressDto> ClassProgress,
        IEnumerable<VideoWatchCountDto> MostWatchedVideos,
        IEnumerable<StudentEngagementDto> StudentEngagement,
        IEnumerable<TopPerformerDto> TopPerformers,
        IEnumerable<ClassLeaderboardDto> ClassLeaderboards
    );

    public record ClassLeaderboardDto(
        Guid ClassId,
        string ClassName,
        IEnumerable<ClassLeaderboardEntryDto> Entries
    );

    public record ClassLeaderboardEntryDto(
        int Rank,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        double CompletedPercentage,
        double WatchHours,
        double VideoStudyMinutes
    );

    public record ClassProgressDto(
        string ClassName,
        double AverageWatchTimeSeconds,
        double CompletionRatePercentage,
        int ActiveStudentsCount,
        int AssignmentsCount
    );

    public record VideoWatchCountDto(
        string VideoTitle,
        int TotalWatchTracks,
        double AverageCompletionPercentage
    );

    public record StudentEngagementDto(
        string StudentName,
        string StudentEmail,
        double TotalWatchTimeHours,
        int CompletedVideosCount,
        int SubmittedAssignmentsCount,
        string ClassName
    );

    public record TopPerformerDto(
        string StudentName,
        string StudentEmail,
        double AverageScorePercentage,
        int GradedAssignmentsCount
    );

    // Admin Analytics
    public record AdminAnalyticsResponse(
        int TotalTeachers,
        int TotalStudents,
        int TotalClasses,
        int TotalVideos,
        int TotalAssignments,
        int TotalSubmissions
    );
}
