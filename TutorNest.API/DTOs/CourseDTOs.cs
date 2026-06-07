using System;
using System.Collections.Generic;

namespace TutorNest.API.DTOs
{
    public record CreateCourseRequest(
        string Title,
        string Description
    );

    public record CourseResponse(
        Guid Id,
        string Title,
        string Description,
        Guid TeacherId,
        string TeacherName,
        int ClassesCount,
        DateTime CreatedAt
    );

    public record AssignClassesRequest(
        List<Guid> ClassIds
    );

    public record CourseProgressResponse(
        double CompletionPercentage,
        bool CertificateIssued,
        string? CertificateCode,
        Guid? CertificateId
    );

    public record CertificateResponse(
        Guid Id,
        string StudentName,
        string StudentEmail,
        Guid? CourseId,
        string? CourseTitle,
        Guid? ClassId,
        string? ClassName,
        string CertificateCode,
        DateTime IssuedAt,
        string? CustomTitle = null,
        string? CustomSubTitle = null,
        string? CustomMessage = null
    );
}
