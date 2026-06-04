namespace TutorNest.API.DTOs
{
    public record CreateVideoRequest(string Title, string Description, string VideoUrl);

    public record VideoResponse(
        Guid Id, 
        string Title, 
        string Description, 
        string VideoUrl, 
        DateTime CreatedAt, 
        Guid TeacherId
    );

    public record AssignVideoRequest(Guid VideoId);
}
