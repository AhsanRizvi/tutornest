using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class ReportService : IReportService
    {
        private readonly TutorNestDbContext _context;

        public ReportService(TutorNestDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerateClassProgressReportAsync(Guid classId, Guid teacherId)
        {
            var @class = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
            
            if (@class == null) throw new KeyNotFoundException("Class not found or does not belong to you.");

            var classStudents = await _context.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Include(cs => cs.Student)
                .ToListAsync();

            var classVideos = await _context.ClassVideos
                .Where(cv => cv.ClassId == classId)
                .Select(cv => cv.VideoId)
                .ToListAsync();

            var classAssignmentsCount = await _context.Assignments
                .CountAsync(a => a.ClassId == classId);

            var rows = new List<string[]>();

            foreach (var cs in classStudents)
            {
                var studentName = $"{cs.Student.FirstName} {cs.Student.LastName}";
                var studentEmail = cs.Student.Email ?? "N/A";

                // Progress Watch metrics
                int completedVideos = 0;
                double watchMins = 0;

                if (classVideos.Any())
                {
                    var progresses = await _context.VideoProgresses
                        .Where(vp => vp.StudentId == cs.StudentId && classVideos.Contains(vp.VideoId))
                        .ToListAsync();

                    completedVideos = progresses.Count(p => p.IsCompleted);
                    watchMins = progresses.Sum(p => p.WatchTimeSeconds) / 60.0;
                }

                // Assignment submissions metrics
                var submissionsCount = await _context.AssignmentSubmissions
                    .CountAsync(sub => sub.StudentId == cs.StudentId && sub.Assignment.ClassId == classId);

                rows.Add(new[]
                {
                    studentName,
                    studentEmail,
                    $"{completedVideos} / {classVideos.Count} completed",
                    $"{Math.Round(watchMins, 1)} mins",
                    $"{submissionsCount} / {classAssignmentsCount} tasks"
                });
            }

            var title = $"Student Progress Report - {@class.Name}";
            var subtitle = $"Generated on {DateTime.UtcNow:f} UTC | Assigned Videos: {classVideos.Count} | Total Homework Tasks: {classAssignmentsCount}";
            var headers = new[] { "Student Name", "Email Address", "Videos Completed", "Streaming Duration", "Homework Submissions" };

            return SimplePdfReport.Generate(title, subtitle, headers, rows);
        }

        public async Task<byte[]> GenerateAssignmentResultsReportAsync(Guid assignmentId, Guid teacherId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && a.Class.TeacherId == teacherId);

            if (assignment == null) throw new KeyNotFoundException("Assignment not found or does not belong to you.");

            var classStudents = await _context.ClassStudents
                .Where(cs => cs.ClassId == assignment.ClassId)
                .Include(cs => cs.Student)
                .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
                .Where(sub => sub.AssignmentId == assignmentId)
                .ToDictionaryAsync(sub => sub.StudentId);

            var rows = new List<string[]>();

            foreach (var cs in classStudents)
            {
                var studentName = $"{cs.Student.FirstName} {cs.Student.LastName}";
                var studentEmail = cs.Student.Email ?? "N/A";
                
                var submitted = submissions.TryGetValue(cs.StudentId, out var subRecord);
                var status = submitted ? "Submitted" : "Pending";
                var score = "N/A";
                var date = "N/A";
                var graded = "No";

                if (submitted && subRecord != null)
                {
                    date = subRecord.SubmittedAt.ToString("g");
                    if (subRecord.Grade.HasValue)
                    {
                        score = $"{subRecord.Grade.Value} / {assignment.TotalMarks}";
                        graded = "Yes";
                    }
                    else
                    {
                        score = "Ungraded";
                    }
                }

                rows.Add(new[]
                {
                    studentName,
                    studentEmail,
                    status,
                    score,
                    date,
                    graded
                });
            }

            var title = $"Homework Grades Summary - {assignment.Title}";
            var subtitle = $"Class: {assignment.Class.Name} | Due Date: {assignment.DueDate:g} | Total Marks: {assignment.TotalMarks}";
            var headers = new[] { "Student Name", "Email Address", "Submission Status", "Grade Earned", "Submitted Date", "Graded" };

            return SimplePdfReport.Generate(title, subtitle, headers, rows);
        }

        public async Task<byte[]> GenerateAdminPlatformReportAsync()
        {
            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == ApplicationRole.Teacher);
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == ApplicationRole.Student);

            var teacherRoleId = teacherRole?.Id ?? Guid.Empty;
            var studentRoleId = studentRole?.Id ?? Guid.Empty;

            var totalTeachers = await _context.UserRoles.CountAsync(ur => ur.RoleId == teacherRoleId);
            var totalStudents = await _context.UserRoles.CountAsync(ur => ur.RoleId == studentRoleId);
            var totalClasses = await _context.Classes.CountAsync();
            var totalVideos = await _context.Videos.CountAsync();
            var totalAssignments = await _context.Assignments.CountAsync();
            var totalSubmissions = await _context.AssignmentSubmissions.CountAsync();

            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.EnrolledStudents)
                .ToListAsync();

            var rows = new List<string[]>();

            foreach (var c in classes)
            {
                var teacherName = c.Teacher != null ? $"{c.Teacher.FirstName} {c.Teacher.LastName}" : "Unknown";
                var studentCount = c.EnrolledStudents.Count;
                var assignmentsCount = await _context.Assignments.CountAsync(a => a.ClassId == c.Id);

                rows.Add(new[]
                {
                    c.Name,
                    teacherName,
                    $"{studentCount} students",
                    $"{assignmentsCount} tasks",
                    c.CreatedAt.ToString("d")
                });
            }

            var title = "TutorNest LMS - Platform Performance Summary";
            var subtitle = $"Generated on {DateTime.UtcNow:g} UTC | Teachers: {totalTeachers} | Students: {totalStudents} | Total Submissions: {totalSubmissions}";
            var headers = new[] { "Classroom Name", "Instructor Tutor", "Students Mapped", "Assignments", "Created Date" };

            return SimplePdfReport.Generate(title, subtitle, headers, rows);
        }

        public async Task<byte[]> GenerateCertificatePdfAsync(Guid certificateId)
        {
            var ct = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Class)
                .FirstOrDefaultAsync(c => c.Id == certificateId);

            if (ct == null) throw new KeyNotFoundException("Certificate not found.");

            var title = "TUTORNEST ACADEMY CERTIFICATE OF COMPLETION";
            var subtitle = "Official Document of Academic Completion Verification";
            var headers = new[] { "Award Item", "Awardee Verification Details" };

            var rows = new List<string[]>
            {
                new[] { "Recipient Student Name", $"{ct.Student.FirstName} {ct.Student.LastName}" },
                new[] { "Registered Student Email", ct.Student.Email ?? "N/A" },
                new[] { "Completed Curriculum", ct.Course != null ? ct.Course.Title : ct.Class!.Name },
                new[] { "Issued Date", ct.IssuedAt.ToString("D") },
                // Unique code
                new[] { "Verification ID Code", ct.CertificateCode }
            };

            return SimplePdfReport.Generate(title, subtitle, headers, rows);
        }

        public async Task<byte[]> GenerateAdminRevenueReportAsync()
        {
            var totalRevenue = await _context.PaymentHistories
                .Where(ph => ph.Status == "Paid")
                .SumAsync(ph => ph.Amount);

            var activeSubsCount = await _context.TeacherSubscriptions
                .Where(ts => ts.Status == "Active")
                .CountAsync();

            var totalTransactionsCount = await _context.PaymentHistories.CountAsync();

            var transactions = await _context.PaymentHistories
                .Include(ph => ph.Teacher)
                .Include(ph => ph.SubscriptionPlan)
                .OrderByDescending(ph => ph.PaymentDate)
                .ToListAsync();

            var rows = new List<string[]>();
            foreach (var ph in transactions)
            {
                var teacherName = ph.Teacher != null ? $"{ph.Teacher.FirstName} {ph.Teacher.LastName}" : "Unknown";
                var teacherEmail = ph.Teacher?.Email ?? "N/A";
                var planName = ph.SubscriptionPlan?.Name ?? "N/A";
                var amountStr = $"{ph.Amount:F2} {ph.Currency}";
                var dateStr = ph.PaymentDate.ToString("g");

                rows.Add(new[]
                {
                    teacherName,
                    teacherEmail,
                    planName,
                    amountStr,
                    ph.Status,
                    ph.PaymentProvider,
                    ph.TransactionId,
                    dateStr
                });
            }

            var title = "TutorNest - Administrative Revenue & Subscription Report";
            var subtitle = $"Generated on {DateTime.UtcNow:g} UTC | Total Paid Revenue: {totalRevenue:F2} USD | Active Subscriptions: {activeSubsCount} | Total Trans.: {totalTransactionsCount}";
            var headers = new[] { "Teacher Name", "Email Address", "Sub Plan", "Amount Paid", "Status", "Provider", "Txn ID", "Payment Date" };

            return SimplePdfReport.Generate(title, subtitle, headers, rows);
        }
    }
}
