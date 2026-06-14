using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class StudentService : IStudentService
    {
        private readonly TutorNestDbContext _context;

        public StudentService(TutorNestDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClassResponse>> GetMyClassesAsync(Guid studentId)
        {
            return await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId)
                .Select(cs => new ClassResponse(
                    cs.Class.Id,
                    cs.Class.Name,
                    cs.Class.Description,
                    cs.Class.CreatedAt,
                    cs.Class.TeacherId,
                    cs.Class.EnrolledStudents.Count,
                    cs.Class.CourseId
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<StudentVideoResponse>> GetClassVideosAsync(Guid classId, Guid studentId)
        {
            // Verify student is enrolled in this class
            var isEnrolled = await _context.ClassStudents.AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (!isEnrolled)
            {
                throw new KeyNotFoundException("Class not found or you are not enrolled in it.");
            }

            // Retrieve class videos alongside progress for this student
            return await _context.ClassVideos
                .Where(cv => cv.ClassId == classId)
                .Select(cv => new
                {
                    Video = cv.Video,
                    Progress = cv.Video.Progresses.FirstOrDefault(p => p.StudentId == studentId)
                })
                .Select(x => new StudentVideoResponse(
                    x.Video.Id,
                    x.Video.Title,
                    x.Video.Description,
                    x.Video.VideoUrl,
                    x.Video.CreatedAt,
                    x.Progress != null ? x.Progress.WatchTimeSeconds : 0,
                    x.Progress != null ? x.Progress.DurationSeconds : 0,
                    x.Progress != null ? x.Progress.IsCompleted : false,
                    x.Progress != null ? (DateTime?)x.Progress.LastWatchedAt : null
                ))
                .ToListAsync();
        }

        public async Task<ProgressResponse> UpdateProgressAsync(Guid videoId, Guid studentId, UpdateProgressRequest request)
        {
            // Verify student is assigned this video in one of their classes
            var isAssigned = await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId)
                .AnyAsync(cs => cs.Class.AssignedVideos.Any(cv => cv.VideoId == videoId));

            if (!isAssigned)
            {
                throw new InvalidOperationException("You do not have access to this video.");
            }

            var progress = await _context.VideoProgresses
                .FirstOrDefaultAsync(vp => vp.StudentId == studentId && vp.VideoId == videoId);

            if (progress == null)
            {
                progress = new VideoProgress
                {
                    StudentId = studentId,
                    VideoId = videoId,
                    WatchTimeSeconds = request.WatchTimeSeconds,
                    DurationSeconds = request.DurationSeconds,
                    IsCompleted = request.IsCompleted,
                    LastWatchedAt = DateTime.UtcNow
                };
                _context.VideoProgresses.Add(progress);
            }
            else
            {
                progress.WatchTimeSeconds = request.WatchTimeSeconds;
                progress.DurationSeconds = request.DurationSeconds;
                // If it was already completed, keep it completed
                progress.IsCompleted = progress.IsCompleted || request.IsCompleted;
                progress.LastWatchedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new ProgressResponse(
                VideoId: progress.VideoId,
                WatchTimeSeconds: progress.WatchTimeSeconds,
                DurationSeconds: progress.DurationSeconds,
                IsCompleted: progress.IsCompleted,
                LastWatchedAt: progress.LastWatchedAt
            );
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetClassLeaderboardAsync(Guid classId, Guid studentId)
        {
            // Verify student is enrolled in this class
            var isEnrolled = await _context.ClassStudents.AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (!isEnrolled)
            {
                throw new KeyNotFoundException("Class not found or you are not enrolled in it.");
            }

            var classVideoIds = await _context.ClassVideos
                .Where(cv => cv.ClassId == classId)
                .Select(cv => cv.VideoId)
                .ToListAsync();

            var leaderboardData = await _context.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new
                {
                    StudentId = cs.StudentId,
                    StudentName = cs.Student.FirstName + " " + cs.Student.LastName,
                    WatchTime = _context.VideoProgresses
                        .Where(vp => vp.StudentId == cs.StudentId && classVideoIds.Contains(vp.VideoId))
                        .Sum(vp => (double?)vp.WatchTimeSeconds) ?? 0.0,
                    SubmissionsCount = _context.AssignmentSubmissions
                        .Count(sub => sub.StudentId == cs.StudentId && sub.Assignment.ClassId == classId),
                    CompletedCount = _context.VideoProgresses
                        .Count(vp => vp.StudentId == cs.StudentId && classVideoIds.Contains(vp.VideoId) && vp.IsCompleted)
                })
                .ToListAsync();

            var sortedList = leaderboardData
                .Select(d => {
                    double completedPct = classVideoIds.Any()
                        ? ((double)d.CompletedCount / classVideoIds.Count) * 100.0
                        : 0.0;
                    double watchHours = Math.Round(d.WatchTime / 3600.0, 2);
                    double studyMinutes = Math.Round(d.WatchTime / 60.0, 1);

                    return new LeaderboardEntry(
                        Rank: 0,
                        StudentId: d.StudentId,
                        StudentName: d.StudentName,
                        VideoWatchTimeSeconds: d.WatchTime,
                        AssignmentsSubmittedCount: d.SubmissionsCount,
                        TotalScoreTimeSeconds: d.WatchTime + (d.SubmissionsCount * 3600.0),
                        CompletedPercentage: Math.Round(completedPct, 1),
                        WatchHours: watchHours,
                        VideoStudyMinutes: studyMinutes
                    );
                })
                .OrderByDescending(e => e.TotalScoreTimeSeconds)
                .ToList();

            for (int i = 0; i < sortedList.Count; i++)
            {
                sortedList[i] = sortedList[i] with { Rank = i + 1 };
            }

            return sortedList;
        }
    }
}
