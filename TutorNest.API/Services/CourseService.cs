using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class CourseService : ICourseService
    {
        private readonly TutorNestDbContext _context;

        public CourseService(TutorNestDbContext context)
        {
            _context = context;
        }

        public async Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, Guid teacherId)
        {
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var teacher = await _context.Users.FirstAsync(u => u.Id == teacherId);

            return new CourseResponse(
                course.Id,
                course.Title,
                course.Description,
                teacherId,
                $"{teacher.FirstName} {teacher.LastName}",
                0,
                course.CreatedAt
            );
        }

        public async Task<IEnumerable<CourseResponse>> GetTeacherCoursesAsync(Guid teacherId)
        {
            return await _context.Courses
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.ClassGroups)
                .Include(c => c.Teacher)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CourseResponse(
                    c.Id,
                    c.Title,
                    c.Description,
                    c.TeacherId,
                    $"{c.Teacher.FirstName} {c.Teacher.LastName}",
                    c.ClassGroups.Count,
                    c.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<CourseResponse> GetCourseByIdAsync(Guid id)
        {
            var c = await _context.Courses
                .Include(c => c.ClassGroups)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (c == null) throw new KeyNotFoundException("Course not found.");

            return new CourseResponse(
                c.Id,
                c.Title,
                c.Description,
                c.TeacherId,
                $"{c.Teacher.FirstName} {c.Teacher.LastName}",
                c.ClassGroups.Count,
                c.CreatedAt
            );
        }

        public async Task AssignClassesToCourseAsync(Guid courseId, List<Guid> classIds, Guid teacherId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);
            if (course == null)
            {
                throw new KeyNotFoundException("Course not found or you do not have permission.");
            }

            // Remove existing associations
            var existingClasses = await _context.Classes.Where(c => c.CourseId == courseId).ToListAsync();
            foreach (var cls in existingClasses)
            {
                cls.CourseId = null;
            }

            // Add new associations
            var classesToAssign = await _context.Classes
                .Where(c => classIds.Contains(c.Id) && c.TeacherId == teacherId)
                .ToListAsync();

            foreach (var cls in classesToAssign)
            {
                cls.CourseId = courseId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<CourseProgressResponse> GetStudentCourseProgressAsync(Guid courseId, Guid studentId)
        {
            var course = await _context.Courses
                .Include(c => c.ClassGroups)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) throw new KeyNotFoundException("Course not found.");

            // 1. Gather all class group IDs in the course
            var classIds = course.ClassGroups.Select(cg => cg.Id).ToList();
            if (classIds.Count == 0)
            {
                return new CourseProgressResponse(0, false, null, null);
            }

            // 2. Query total unique videos assigned to these classes
            var videoIds = await _context.ClassVideos
                .Where(cv => classIds.Contains(cv.ClassId))
                .Select(cv => cv.VideoId)
                .Distinct()
                .ToListAsync();

            if (videoIds.Count == 0)
            {
                // No lessons yet, so progress is technically complete
                return await GetOrIssueCertificateAsync(courseId, studentId, 100);
            }

            // 3. Count how many of these videos are marked completed by the student
            var completedCount = await _context.VideoProgresses
                .Where(vp => vp.StudentId == studentId && videoIds.Contains(vp.VideoId) && vp.IsCompleted)
                .CountAsync();

            double progress = Math.Round(((double)completedCount / videoIds.Count) * 100, 1);

            return await GetOrIssueCertificateAsync(courseId, studentId, progress);
        }

        private async Task<CourseProgressResponse> GetOrIssueCertificateAsync(Guid courseId, Guid studentId, double progress)
        {
            var isComplete = progress >= 100;
            var certificate = await _context.Certificates
                .FirstOrDefaultAsync(ct => ct.CourseId == courseId && ct.StudentId == studentId);

            if (isComplete && certificate == null)
            {
                // Generate a new certificate code
                var certCode = $"CERT-{courseId.ToString().Substring(0, 5).ToUpper()}-{studentId.ToString().Substring(0, 5).ToUpper()}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
                
                certificate = new Certificate
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    CourseId = courseId,
                    CertificateCode = certCode,
                    IssuedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();
            }

            return new CourseProgressResponse(
                progress,
                certificate != null,
                certificate?.CertificateCode,
                certificate?.Id
            );
        }

        public async Task<CertificateResponse> GetCertificateAsync(Guid certificateId)
        {
            var ct = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Course)
                .Include(c => c.Class)
                .FirstOrDefaultAsync(c => c.Id == certificateId);

            if (ct == null) throw new KeyNotFoundException("Certificate not found.");

            return new CertificateResponse(
                ct.Id,
                $"{ct.Student.FirstName} {ct.Student.LastName}",
                ct.Student.Email!,
                ct.CourseId,
                ct.Course?.Title,
                ct.ClassId,
                ct.Class?.Name,
                ct.CertificateCode,
                ct.IssuedAt,
                ct.CustomTitle,
                ct.CustomSubTitle,
                ct.CustomMessage
            );
        }

        public async Task<IEnumerable<CertificateResponse>> GetStudentCertificatesAsync(Guid studentId)
        {
            return await _context.Certificates
                .Where(ct => ct.StudentId == studentId)
                .Include(ct => ct.Course)
                .Include(ct => ct.Class)
                .Include(ct => ct.Student)
                .OrderByDescending(ct => ct.IssuedAt)
                .Select(ct => new CertificateResponse(
                    ct.Id,
                    $"{ct.Student.FirstName} {ct.Student.LastName}",
                    ct.Student.Email!,
                    ct.CourseId,
                    ct.Course != null ? ct.Course.Title : null,
                    ct.ClassId,
                    ct.Class != null ? ct.Class.Name : null,
                    ct.CertificateCode,
                    ct.IssuedAt,
                    ct.CustomTitle,
                    ct.CustomSubTitle,
                    ct.CustomMessage
                ))
                .ToListAsync();
        }
    }
}
