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
    }
}
