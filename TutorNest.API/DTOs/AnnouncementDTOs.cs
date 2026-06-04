namespace TutorNest.API.DTOs
{
    public record CreateAnnouncementRequest(
        string Title,
        string Content,
        string? AttachmentUrl,
        Guid? ClassId
    );

    public record AnnouncementResponse(
        Guid Id,
        string Title,
        string Content,
        string? AttachmentUrl,
        Guid TeacherId,
        string TeacherName,
        Guid? ClassId,
        string? ClassName,
        DateTime CreatedAt,
        bool IsRead
    );
}
