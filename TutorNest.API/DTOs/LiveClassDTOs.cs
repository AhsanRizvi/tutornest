using System;

namespace TutorNest.API.DTOs
{
    public record CreateLiveClassRequest(
        string Title,
        string Description,
        DateTime ScheduledStartTime,
        int DurationMinutes,
        string MeetingLink,
        Guid ClassId
    );

    public record LiveClassResponse(
        Guid Id,
        string Title,
        string Description,
        DateTime ScheduledStartTime,
        int DurationMinutes,
        string MeetingLink,
        string? RecordingUrl,
        Guid ClassId,
        string ClassName,
        Guid TeacherId,
        string TeacherName,
        DateTime CreatedAt
    );

    public record UploadRecordingRequest(
        string RecordingUrl
    );
}
