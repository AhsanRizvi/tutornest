using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly TutorNestDbContext _context;

        public TeacherService(TutorNestDbContext context)
        {
            _context = context;
        }

        public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, Guid teacherId)
        {
            var @class = new Class
            {
                Name = request.Name,
                Description = request.Description,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Classes.Add(@class);
            await _context.SaveChangesAsync();

            return new ClassResponse(@class.Id, @class.Name, @class.Description, @class.CreatedAt, @class.TeacherId, 0, null);
        }

        public async Task<IEnumerable<ClassResponse>> GetClassesAsync(Guid teacherId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == teacherId)
                .Select(c => new ClassResponse(
                    c.Id, 
                    c.Name, 
                    c.Description, 
                    c.CreatedAt, 
                    c.TeacherId, 
                    c.EnrolledStudents.Count,
                    c.CourseId
                ))
                .ToListAsync();
        }

        public async Task<bool> EnrollStudentAsync(Guid classId, Guid studentId, Guid teacherId)
        {
            // Verify class belongs to teacher
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            // Verify student is mapped to teacher
            var isStudentMapped = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (!isStudentMapped)
            {
                throw new InvalidOperationException("Student does not belong to you.");
            }

            // Check if student is already enrolled
            var isEnrolled = await _context.ClassStudents.AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (isEnrolled)
            {
                return true;
            }

            var classStudent = new ClassStudent
            {
                ClassId = classId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.ClassStudents.Add(classStudent);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<StudentResponse>> GetClassStudentsAsync(Guid classId, Guid teacherId)
        {
            // Verify class belongs to teacher
            var @class = await _context.Classes.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (!@class)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            return await _context.ClassStudents
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new StudentResponse(
                    cs.Student.Id,
                    cs.Student.Email!,
                    cs.Student.FirstName,
                    cs.Student.LastName
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<StudentResponse>> GetMyStudentsAsync(Guid teacherId)
        {
            return await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => new StudentResponse(
                    ts.Student.Id,
                    ts.Student.Email!,
                    ts.Student.FirstName,
                    ts.Student.LastName
                ))
                .ToListAsync();
        }

        public async Task<VideoResponse> CreateVideoAsync(CreateVideoRequest request, Guid teacherId)
        {
            var video = new Video
            {
                Title = request.Title,
                Description = request.Description,
                VideoUrl = request.VideoUrl,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            return new VideoResponse(video.Id, video.Title, video.Description, video.VideoUrl, video.CreatedAt, video.TeacherId);
        }

        public async Task<IEnumerable<VideoResponse>> GetMyVideosAsync(Guid teacherId)
        {
            return await _context.Videos
                .Where(v => v.TeacherId == teacherId)
                .Select(v => new VideoResponse(
                    v.Id, 
                    v.Title, 
                    v.Description, 
                    v.VideoUrl, 
                    v.CreatedAt, 
                    v.TeacherId
                ))
                .ToListAsync();
        }

        public async Task<bool> AssignVideoToClassAsync(Guid classId, Guid videoId, Guid teacherId)
        {
            // Verify class belongs to teacher
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (!classExists)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            // Verify video belongs to teacher
            var videoExists = await _context.Videos.AnyAsync(v => v.Id == videoId && v.TeacherId == teacherId);
            if (!videoExists)
            {
                throw new KeyNotFoundException("Video not found or does not belong to you.");
            }

            // Check if already assigned
            var isAssigned = await _context.ClassVideos.AnyAsync(cv => cv.ClassId == classId && cv.VideoId == videoId);
            if (isAssigned)
            {
                return true;
            }

            var classVideo = new ClassVideo
            {
                ClassId = classId,
                VideoId = videoId,
                AssignedAt = DateTime.UtcNow
            };

            _context.ClassVideos.Add(classVideo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<VideoResponse>> GetClassVideosAsync(Guid classId, Guid teacherId)
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (!classExists)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            return await _context.ClassVideos
                .Where(cv => cv.ClassId == classId)
                .Select(cv => new VideoResponse(
                    cv.Video.Id,
                    cv.Video.Title,
                    cv.Video.Description,
                    cv.Video.VideoUrl,
                    cv.Video.CreatedAt,
                    cv.Video.TeacherId
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<StudentProgressReport>> GetStudentsProgressAsync(Guid teacherId)
        {
            // Get all progress rows where the video belongs to this teacher and the student belongs to this teacher
            return await _context.VideoProgresses
                .Where(vp => vp.Video.TeacherId == teacherId && vp.Student.TeacherStudents.Any(ts => ts.TeacherId == teacherId))
                .Select(vp => new StudentProgressReport(
                    vp.StudentId,
                    $"{vp.Student.FirstName} {vp.Student.LastName}",
                    vp.Student.Email!,
                    vp.VideoId,
                    vp.Video.Title,
                    vp.WatchTimeSeconds,
                    vp.DurationSeconds,
                    vp.IsCompleted,
                    vp.LastWatchedAt
                ))
                .OrderByDescending(vp => vp.LastWatchedAt)
                .ToListAsync();
        }
    }
}
