using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly TutorNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsService(TutorNestDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<TeacherAnalyticsResponse> GetTeacherAnalyticsAsync(Guid teacherId)
        {
            // 1. Class progress DTOs
            var classes = await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.EnrolledStudents)
                .ToListAsync();

            var classProgressList = new List<ClassProgressDto>();
            foreach (var c in classes)
            {
                var studentIdsInClass = c.EnrolledStudents.Select(es => es.StudentId).ToList();
                var videoIdsInClass = await _context.ClassVideos
                    .Where(cv => cv.ClassId == c.Id)
                    .Select(cv => cv.VideoId)
                    .ToListAsync();

                double avgWatchTime = 0;
                double completionPercentage = 0;
                
                if (studentIdsInClass.Any() && videoIdsInClass.Any())
                {
                    var progresses = await _context.VideoProgresses
                        .Where(vp => studentIdsInClass.Contains(vp.StudentId) && videoIdsInClass.Contains(vp.VideoId))
                        .ToListAsync();

                    if (progresses.Any())
                    {
                        avgWatchTime = progresses.Average(p => p.WatchTimeSeconds);
                        var completedCount = progresses.Count(p => p.IsCompleted);
                        completionPercentage = (double)completedCount / (studentIdsInClass.Count * videoIdsInClass.Count) * 100.0;
                    }
                }

                var assignmentsCount = await _context.Assignments.CountAsync(a => a.ClassId == c.Id);

                classProgressList.Add(new ClassProgressDto(
                    ClassName: c.Name,
                    AverageWatchTimeSeconds: Math.Round(avgWatchTime, 1),
                    CompletionRatePercentage: Math.Min(100.0, Math.Round(completionPercentage, 1)),
                    ActiveStudentsCount: studentIdsInClass.Count,
                    AssignmentsCount: assignmentsCount
                ));
            }

            // 2. Most watched videos (Top 5)
            var progressData = await _context.VideoProgresses
                .Where(vp => vp.Video.TeacherId == teacherId)
                .Select(vp => new {
                    vp.VideoId,
                    VideoTitle = vp.Video.Title,
                    vp.WatchTimeSeconds,
                    vp.DurationSeconds
                })
                .ToListAsync();

            var mostWatchedVideos = progressData
                .GroupBy(vp => new { vp.VideoId, vp.VideoTitle })
                .Select(g => new VideoWatchCountDto(
                    g.Key.VideoTitle,
                    g.Count(),
                    g.Average(p => p.DurationSeconds > 0 ? (p.WatchTimeSeconds / p.DurationSeconds) * 100.0 : 0)
                ))
                .OrderByDescending(v => v.TotalWatchTracks)
                .Take(5)
                .ToList();

            // 3. Student Engagement summary
            var students = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.Student)
                .ToListAsync();

            var studentEngagementList = new List<StudentEngagementDto>();
            foreach (var s in students)
            {
                var watchProgresses = await _context.VideoProgresses
                    .Where(vp => vp.StudentId == s.Id && vp.Video.TeacherId == teacherId)
                    .ToListAsync();

                var watchHours = watchProgresses.Sum(p => p.WatchTimeSeconds) / 3600.0;
                var completedVideos = watchProgresses.Count(p => p.IsCompleted);

                // Submitted assignments for classes belonging to this teacher
                var submissionsCount = await _context.AssignmentSubmissions
                    .CountAsync(sub => sub.StudentId == s.Id && sub.Assignment.Class.TeacherId == teacherId);

                var classNames = await _context.ClassStudents
                    .Where(cs => cs.StudentId == s.Id && cs.Class.TeacherId == teacherId)
                    .Select(cs => cs.Class.Name)
                    .ToListAsync();
                var className = classNames.Any() ? string.Join(", ", classNames) : "N/A";

                studentEngagementList.Add(new StudentEngagementDto(
                    StudentName: $"{s.FirstName} {s.LastName}",
                    StudentEmail: s.Email!,
                    TotalWatchTimeHours: Math.Round(watchHours, 2),
                    CompletedVideosCount: completedVideos,
                    SubmittedAssignmentsCount: submissionsCount,
                    ClassName: className
                ));
            }

            // 4. Top performers (Students sorted by average grade score on graded assignments)
            var submissionData = await _context.AssignmentSubmissions
                .Where(sub => sub.Assignment.Class.TeacherId == teacherId && sub.Grade.HasValue)
                .Select(sub => new {
                    sub.StudentId,
                    StudentFirstName = sub.Student.FirstName,
                    StudentLastName = sub.Student.LastName,
                    StudentEmail = sub.Student.Email,
                    Grade = sub.Grade!.Value,
                    TotalMarks = sub.Assignment.TotalMarks
                })
                .ToListAsync();

            var topPerformers = submissionData
                .GroupBy(sub => new { sub.StudentId, sub.StudentFirstName, sub.StudentLastName, sub.StudentEmail })
                .Select(g => new TopPerformerDto(
                    $"{g.Key.StudentFirstName} {g.Key.StudentLastName}",
                    g.Key.StudentEmail!,
                    g.Average(sub => (sub.Grade / sub.TotalMarks) * 100.0),
                    g.Count()
                ))
                .OrderByDescending(tp => tp.AverageScorePercentage)
                .Take(5)
                .ToList();

            // 5. Class Wise Leaderboard
            var classLeaderboards = new List<ClassLeaderboardDto>();
            foreach (var c in classes)
            {
                var studentIdsInClass = c.EnrolledStudents.Select(es => es.StudentId).ToList();
                var videoIdsInClass = await _context.ClassVideos
                    .Where(cv => cv.ClassId == c.Id)
                    .Select(cv => cv.VideoId)
                    .ToListAsync();

                var entries = new List<ClassLeaderboardEntryDto>();

                var classStudents = await _context.ClassStudents
                    .Where(cs => cs.ClassId == c.Id)
                    .Select(cs => new {
                        cs.StudentId,
                        StudentName = $"{cs.Student.FirstName} {cs.Student.LastName}",
                        StudentEmail = cs.Student.Email
                    })
                    .ToListAsync();

                foreach (var cs in classStudents)
                {
                    double completedPct = 0;
                    double watchHours = 0;
                    double studyMinutes = 0;
                    if (videoIdsInClass.Any())
                    {
                        var watchProgresses = await _context.VideoProgresses
                            .Where(vp => vp.StudentId == cs.StudentId && videoIdsInClass.Contains(vp.VideoId))
                            .ToListAsync();

                        var completedCount = watchProgresses.Count(vp => vp.IsCompleted);
                        completedPct = (double)completedCount / videoIdsInClass.Count * 100.0;

                        var totalWatchSeconds = watchProgresses.Sum(vp => vp.WatchTimeSeconds);
                        watchHours = (double)totalWatchSeconds / 3600.0;
                        studyMinutes = (double)totalWatchSeconds / 60.0;
                    }
                    entries.Add(new ClassLeaderboardEntryDto(
                        Rank: 0,
                        StudentId: cs.StudentId,
                        StudentName: cs.StudentName,
                        StudentEmail: cs.StudentEmail ?? "",
                        CompletedPercentage: Math.Round(completedPct, 1),
                        WatchHours: Math.Round(watchHours, 2),
                        VideoStudyMinutes: Math.Round(studyMinutes, 1)
                    ));
                }

                var sortedEntries = entries
                    .OrderByDescending(e => e.CompletedPercentage)
                    .ThenBy(e => e.StudentName)
                    .ToList();

                for (int i = 0; i < sortedEntries.Count; i++)
                {
                    sortedEntries[i] = sortedEntries[i] with { Rank = i + 1 };
                }

                classLeaderboards.Add(new ClassLeaderboardDto(
                    ClassId: c.Id,
                    ClassName: c.Name,
                    Entries: sortedEntries
                ));
            }

            return new TeacherAnalyticsResponse(
                ClassProgress: classProgressList,
                MostWatchedVideos: mostWatchedVideos,
                StudentEngagement: studentEngagementList,
                TopPerformers: topPerformers,
                ClassLeaderboards: classLeaderboards
            );
        }

        public async Task<AdminAnalyticsResponse> GetAdminAnalyticsAsync()
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

            return new AdminAnalyticsResponse(
                TotalTeachers: totalTeachers,
                TotalStudents: totalStudents,
                TotalClasses: totalClasses,
                TotalVideos: totalVideos,
                TotalAssignments: totalAssignments,
                TotalSubmissions: totalSubmissions
            );
        }
    }
}
