using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly TutorNestDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStorageService _storageService;

        public TeacherService(TutorNestDbContext context, UserManager<ApplicationUser> userManager, IStorageService storageService)
        {
            _context = context;
            _userManager = userManager;
            _storageService = storageService;
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

        public async Task<ClassResponse> UpdateClassAsync(Guid classId, CreateClassRequest request, Guid teacherId)
        {
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }
            @class.Name = request.Name;
            @class.Description = request.Description;
            await _context.SaveChangesAsync();

            var studentCount = await _context.ClassStudents.CountAsync(cs => cs.ClassId == classId);
            return new ClassResponse(@class.Id, @class.Name, @class.Description, @class.CreatedAt, @class.TeacherId, studentCount, @class.CourseId);
        }

        public async Task DeleteClassAsync(Guid classId, Guid teacherId)
        {
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();
        }

        public async Task<StudentResponse> UpdateStudentAsync(Guid studentId, UpdateStudentRequest request, Guid teacherId)
        {
            var isAssociated = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (!isAssociated)
            {
                throw new KeyNotFoundException("Student not found or does not belong to you.");
            }

            var student = await _userManager.FindByIdAsync(studentId.ToString());
            if (student == null)
            {
                throw new KeyNotFoundException("Student account not found.");
            }

            student.Email = request.Email;
            student.UserName = request.Email;
            student.NormalizedEmail = request.Email.ToUpperInvariant();
            student.NormalizedUserName = request.Email.ToUpperInvariant();
            student.FirstName = request.FirstName;
            student.LastName = request.LastName;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                student.PasswordHash = _userManager.PasswordHasher.HashPassword(student, request.Password);
            }

            var updateResult = await _userManager.UpdateAsync(student);
            if (!updateResult.Succeeded)
            {
                throw new Exception(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }

            return new StudentResponse(student.Id, student.Email, student.FirstName, student.LastName);
        }

        public async Task DeleteStudentAsync(Guid studentId, Guid teacherId)
        {
            var isAssociated = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (!isAssociated)
            {
                throw new KeyNotFoundException("Student not found or does not belong to you.");
            }

            var student = await _userManager.FindByIdAsync(studentId.ToString());
            if (student == null)
            {
                throw new KeyNotFoundException("Student account not found.");
            }

            var result = await _userManager.DeleteAsync(student);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        public async Task RemoveStudentFromClassAsync(Guid classId, Guid studentId, Guid teacherId)
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId && c.TeacherId == teacherId);
            if (!classExists)
            {
                throw new KeyNotFoundException("Class not found or does not belong to you.");
            }

            var classStudent = await _context.ClassStudents.FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
            if (classStudent == null)
            {
                throw new KeyNotFoundException("Student is not enrolled in this class.");
            }

            _context.ClassStudents.Remove(classStudent);
            await _context.SaveChangesAsync();
        }

        public async Task<CertificateResponse> AwardCertificateAsync(AwardCertificateRequest request, Guid teacherId)
        {
            // Verify student is mapped to teacher
            var isStudentMapped = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == request.StudentId);
            if (!isStudentMapped)
            {
                throw new InvalidOperationException("Student does not belong to you.");
            }

            // Verify class/course if provided
            if (request.ClassId.HasValue)
            {
                var classExists = await _context.Classes.AnyAsync(c => c.Id == request.ClassId.Value && c.TeacherId == teacherId);
                if (!classExists)
                {
                    throw new KeyNotFoundException("Class not found or does not belong to you.");
                }
            }

            if (request.CourseId.HasValue)
            {
                var courseExists = await _context.Courses.AnyAsync(co => co.Id == request.CourseId.Value && co.TeacherId == teacherId);
                if (!courseExists)
                {
                    throw new KeyNotFoundException("Course not found or does not belong to you.");
                }
            }

            // Generate a certificate code
            var targetId = request.CourseId?.ToString() ?? request.ClassId?.ToString() ?? "MANUAL";
            var idPart = targetId.Length >= 5 ? targetId.Substring(0, 5) : targetId;
            var studentIdPart = request.StudentId.ToString().Substring(0, 5);
            var randPart = Guid.NewGuid().ToString().Substring(0, 6);
            var certCode = $"CERT-{idPart.ToUpper()}-{studentIdPart.ToUpper()}-{randPart.ToUpper()}";

            var certificate = new Certificate
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                CourseId = request.CourseId,
                ClassId = request.ClassId,
                CertificateCode = certCode,
                IssuedAt = DateTime.UtcNow,
                CustomTitle = request.CustomTitle,
                CustomSubTitle = request.CustomSubTitle,
                CustomMessage = request.CustomMessage
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            // Load student and navigation properties to map to CertificateResponse
            var student = await _context.Users.FirstAsync(u => u.Id == request.StudentId);
            var course = request.CourseId.HasValue ? await _context.Courses.FindAsync(request.CourseId.Value) : null;
            var @class = request.ClassId.HasValue ? await _context.Classes.FindAsync(request.ClassId.Value) : null;

            return new CertificateResponse(
                certificate.Id,
                $"{student.FirstName} {student.LastName}",
                student.Email!,
                certificate.CourseId,
                course?.Title,
                certificate.ClassId,
                @class?.Name,
                certificate.CertificateCode,
                certificate.IssuedAt,
                certificate.CustomTitle,
                certificate.CustomSubTitle,
                certificate.CustomMessage
            );
        }

        public async Task<IEnumerable<CertificateResponse>> GetTeacherCertificatesAsync(Guid teacherId)
        {
            var studentIds = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.StudentId)
                .ToListAsync();

            return await _context.Certificates
                .Where(ct => studentIds.Contains(ct.StudentId))
                .Include(ct => ct.Student)
                .Include(ct => ct.Course)
                .Include(ct => ct.Class)
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

        public async Task DeleteCertificateAsync(Guid certificateId, Guid teacherId)
        {
            var certificate = await _context.Certificates
                .Include(ct => ct.Student)
                .FirstOrDefaultAsync(ct => ct.Id == certificateId);

            if (certificate == null)
            {
                throw new KeyNotFoundException("Certificate not found.");
            }

            // Verify certificate student belongs to teacher
            var isStudentMapped = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == certificate.StudentId);
            if (!isStudentMapped)
            {
                throw new InvalidOperationException("You do not have permission to delete this certificate.");
            }

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVideoAsync(Guid videoId, Guid teacherId)
        {
            var video = await _context.Videos.FirstOrDefaultAsync(v => v.Id == videoId && v.TeacherId == teacherId);
            if (video == null)
            {
                throw new KeyNotFoundException("Video not found or does not belong to you.");
            }

            // Clean up from storage if it is an uploaded file
            if (video.VideoUrl != null && video.VideoUrl.Contains("/videos/"))
            {
                var index = video.VideoUrl.IndexOf("videos/");
                if (index >= 0)
                {
                    var fileKey = video.VideoUrl.Substring(index);
                    try
                    {
                        await _storageService.DeleteAsync(fileKey);
                    }
                    catch
                    {
                        // Ignore storage deletion errors to ensure DB record is still deleted
                    }
                }
            }

            _context.Videos.Remove(video);
            await _context.SaveChangesAsync();
        }
    }
}
