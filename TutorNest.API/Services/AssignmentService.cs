using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly TutorNestDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public AssignmentService(
            TutorNestDbContext context,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<AssignmentResponse> CreateAssignmentAsync(CreateAssignmentRequest request, Guid teacherId)
        {
            // Verify class belongs to teacher
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            var assignment = new Assignment
            {
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate.ToUniversalTime(),
                TotalMarks = request.TotalMarks,
                ClassId = request.ClassId,
                Type = request.Type,
                ConfigJson = request.ConfigJson
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Trigger Notifications & Emails to all students in class
            var students = await _context.ClassStudents
                .Where(cs => cs.ClassId == request.ClassId)
                .Select(cs => cs.Student)
                .ToListAsync();

            foreach (var student in students)
            {
                if (student.Email != null)
                {
                    // Create in-app notification
                    await _notificationService.CreateNotificationAsync(
                        student.Id, 
                        $"New assignment '{assignment.Title}' created in {@class.Name}.", 
                        "Assignment"
                    );

                    // Send email notification (mocked)
                    await _emailService.SendEmailAsync(
                        student.Email,
                        $"New Assignment in {@class.Name}: {assignment.Title}",
                        $"<h3>Hello {student.FirstName},</h3>" +
                        $"<p>Your tutor has posted a new assignment in your class <strong>{@class.Name}</strong>.</p>" +
                        $"<p><strong>Title:</strong> {assignment.Title}</p>" +
                        $"<p><strong>Due Date:</strong> {assignment.DueDate:F} UTC</p>" +
                        $"<p><strong>Marks:</strong> {assignment.TotalMarks}</p>" +
                        $"<p>Log in to TutorNest to submit your work.</p>"
                    );
                }
            }

            return new AssignmentResponse(
                assignment.Id,
                assignment.Title,
                assignment.Description,
                assignment.DueDate,
                assignment.TotalMarks,
                assignment.Type,
                assignment.ConfigJson,
                assignment.ClassId,
                IsSubmitted: false,
                ScoreEarned: null,
                IsGraded: false,
                Feedback: null
            );
        }

        public async Task<IEnumerable<AssignmentResponse>> GetClassAssignmentsAsync(Guid classId, Guid userId, string role)
        {
            if (role == ApplicationRole.Teacher)
            {
                // Verify class belongs to teacher
                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId && c.TeacherId == userId);
                if (!classExists) throw new KeyNotFoundException("Class not found or unauthorized.");

                return await _context.Assignments
                    .Where(a => a.ClassId == classId)
                    .Select(a => new AssignmentResponse(
                        a.Id, a.Title, a.Description, a.DueDate, a.TotalMarks, a.Type, a.ConfigJson, a.ClassId, null, null, null, null
                    ))
                    .ToListAsync();
            }
            else
            {
                // Verify student is enrolled in class
                var isEnrolled = await _context.ClassStudents.AnyAsync(cs => cs.ClassId == classId && cs.StudentId == userId);
                if (!isEnrolled) throw new UnauthorizedAccessException("You are not enrolled in this class.");

                // For student, include contextual submission state
                return await _context.Assignments
                    .Where(a => a.ClassId == classId)
                    .Select(a => new
                    {
                        Assignment = a,
                        Submission = a.Submissions.FirstOrDefault(s => s.StudentId == userId)
                    })
                    .Select(x => new AssignmentResponse(
                        x.Assignment.Id,
                        x.Assignment.Title,
                        x.Assignment.Description,
                        x.Assignment.DueDate,
                        x.Assignment.TotalMarks,
                        x.Assignment.Type,
                        x.Assignment.ConfigJson,
                        x.Assignment.ClassId,
                        x.Submission != null,
                        x.Submission != null ? x.Submission.Grade : null,
                        x.Submission != null ? x.Submission.Grade != null : false,
                        x.Submission != null ? x.Submission.Feedback : null
                    ))
                    .ToListAsync();
            }
        }

        public async Task<SubmissionResponse> SubmitAssignmentAsync(Guid assignmentId, Guid studentId, SubmitAssignmentRequest request)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) throw new KeyNotFoundException("Assignment not found.");

            // Verify student is enrolled in the class of this assignment
            var isEnrolled = await _context.ClassStudents.AnyAsync(cs => cs.ClassId == assignment.ClassId && cs.StudentId == studentId);
            if (!isEnrolled) throw new UnauthorizedAccessException("You are not enrolled in this class.");

            // Check if already submitted
            var submission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (submission != null)
            {
                // Update submission
                submission.AnswerText = request.AnswerText;
                submission.AttachmentUrl = request.AttachmentUrl;
                submission.SubmittedAt = DateTime.UtcNow;
                
                // Reset grading if re-submitted
                submission.Grade = null;
                submission.Feedback = null;
                submission.GradedAt = null;
            }
            else
            {
                // Create new submission
                submission = new AssignmentSubmission
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    AnswerText = request.AnswerText,
                    AttachmentUrl = request.AttachmentUrl,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.AssignmentSubmissions.Add(submission);
            }

            await _context.SaveChangesAsync();

            // Trigger notification to the class teacher
            var teacherId = assignment.Class.TeacherId;
            var student = await _context.Users.FindAsync(studentId);
            if (student != null)
            {
                await _notificationService.CreateNotificationAsync(
                    teacherId,
                    $"Student {student.FirstName} {student.LastName} submitted assignment '{assignment.Title}'.",
                    "Assignment"
                );
            }

            return new SubmissionResponse(
                submission.Id,
                submission.AssignmentId,
                assignment.Title,
                submission.StudentId,
                $"{student?.FirstName} {student?.LastName}",
                student?.Email ?? string.Empty,
                submission.AnswerText,
                submission.AttachmentUrl,
                submission.Grade,
                submission.Feedback,
                submission.SubmittedAt,
                submission.GradedAt
            );
        }

        public async Task<IEnumerable<SubmissionResponse>> GetAssignmentSubmissionsAsync(Guid assignmentId, Guid teacherId)
        {
            // Verify assignment class belongs to teacher
            var assignment = await _context.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.Class.TeacherId == teacherId);

            if (assignment == null) throw new KeyNotFoundException("Assignment not found or unauthorized.");

            return await _context.AssignmentSubmissions
                .Where(s => s.AssignmentId == assignmentId)
                .Select(s => new SubmissionResponse(
                    s.Id,
                    s.AssignmentId,
                    assignment.Title,
                    s.StudentId,
                    $"{s.Student.FirstName} {s.Student.LastName}",
                    s.Student.Email!,
                    s.AnswerText,
                    s.AttachmentUrl,
                    s.Grade,
                    s.Feedback,
                    s.SubmittedAt,
                    s.GradedAt
                ))
                .ToListAsync();
        }

        public async Task<SubmissionResponse> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request, Guid teacherId)
        {
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Class)
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.Assignment.Class.TeacherId == teacherId);

            if (submission == null) throw new KeyNotFoundException("Submission not found or unauthorized.");

            submission.Grade = request.Grade;
            submission.Feedback = request.Feedback;
            submission.GradedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Trigger email & notification to student
            if (submission.Student.Email != null)
            {
                await _notificationService.CreateNotificationAsync(
                    submission.StudentId,
                    $"Your assignment '{submission.Assignment.Title}' has been graded: {request.Grade}/{submission.Assignment.TotalMarks}.",
                    "Grade"
                );

                await _emailService.SendEmailAsync(
                    submission.Student.Email,
                    $"Assignment Graded: {submission.Assignment.Title}",
                    $"<h3>Hello {submission.Student.FirstName},</h3>" +
                    $"<p>Your submission for <strong>{submission.Assignment.Title}</strong> in class <strong>{submission.Assignment.Class.Name}</strong> has been graded.</p>" +
                    $"<p><strong>Grade:</strong> {request.Grade} / {submission.Assignment.TotalMarks}</p>" +
                    $"<p><strong>Feedback:</strong> {request.Feedback}</p>" +
                    $"<p>Log in to review detailed analytics.</p>"
                );
            }

            return new SubmissionResponse(
                submission.Id,
                submission.AssignmentId,
                submission.Assignment.Title,
                submission.StudentId,
                $"{submission.Student.FirstName} {submission.Student.LastName}",
                submission.Student.Email ?? string.Empty,
                submission.AnswerText,
                submission.AttachmentUrl,
                submission.Grade,
                submission.Feedback,
                submission.SubmittedAt,
                submission.GradedAt
            );
        }

        public async Task<IEnumerable<SubmissionResponse>> GetStudentSubmissionsAsync(Guid studentId)
        {
            return await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId)
                .Select(s => new SubmissionResponse(
                    s.Id,
                    s.AssignmentId,
                    s.Assignment.Title,
                    s.StudentId,
                    $"{s.Student.FirstName} {s.Student.LastName}",
                    s.Student.Email!,
                    s.AnswerText,
                    s.AttachmentUrl,
                    s.Grade,
                    s.Feedback,
                    s.SubmittedAt,
                    s.GradedAt
                ))
                .ToListAsync();
        }
    }
}
