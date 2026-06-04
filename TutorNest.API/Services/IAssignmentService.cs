using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IAssignmentService
    {
        Task<AssignmentResponse> CreateAssignmentAsync(CreateAssignmentRequest request, Guid teacherId);
        Task<IEnumerable<AssignmentResponse>> GetClassAssignmentsAsync(Guid classId, Guid userId, string role);
        Task<SubmissionResponse> SubmitAssignmentAsync(Guid assignmentId, Guid studentId, SubmitAssignmentRequest request);
        Task<IEnumerable<SubmissionResponse>> GetAssignmentSubmissionsAsync(Guid assignmentId, Guid teacherId);
        Task<SubmissionResponse> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request, Guid teacherId);
        Task<IEnumerable<SubmissionResponse>> GetStudentSubmissionsAsync(Guid studentId);
    }
}
