namespace TutorNest.API.DTOs
{
    public record CreateClassRequest(string Name, string Description);

    public record ClassResponse(
        Guid Id, 
        string Name, 
        string Description, 
        DateTime CreatedAt, 
        Guid TeacherId,
        int StudentCount,
        Guid? CourseId
    );

    public record EnrollStudentRequest(Guid StudentId);
}
