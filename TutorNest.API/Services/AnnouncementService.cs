using Microsoft.EntityFrameworkCore;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly TutorNestDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public AnnouncementService(
            TutorNestDbContext context,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        public async Task<AnnouncementResponse> CreateAnnouncementAsync(CreateAnnouncementRequest request, Guid teacherId)
        {
            // Verify class belongs to teacher (if ClassId is specified)
            string? className = null;
            if (request.ClassId.HasValue)
            {
                var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId.Value && c.TeacherId == teacherId);
                if (@class == null)
                {
                    throw new KeyNotFoundException("Class not found or does not belong to you.");
                }
                className = @class.Name;
            }

            var announcement = new Announcement
            {
                Title = request.Title,
                Content = request.Content,
                AttachmentUrl = request.AttachmentUrl,
                TeacherId = teacherId,
                ClassId = request.ClassId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Resolve target students
            List<ApplicationUser> targetStudents;
            if (request.ClassId.HasValue)
            {
                targetStudents = await _context.ClassStudents
                    .Where(cs => cs.ClassId == request.ClassId.Value)
                    .Select(cs => cs.Student)
                    .ToListAsync();
            }
            else
            {
                targetStudents = await _context.TeacherStudents
                    .Where(ts => ts.TeacherId == teacherId)
                    .Select(ts => ts.Student)
                    .ToListAsync();
            }

            var teacher = await _context.Users.FindAsync(teacherId);
            var teacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Your Tutor";

            // Trigger Notifications & Emails
            foreach (var student in targetStudents)
            {
                if (student.Email != null)
                {
                    // Create in-app alert
                    await _notificationService.CreateNotificationAsync(
                        student.Id,
                        $"New notice '{announcement.Title}' posted by {teacherName}.",
                        "Announcement"
                    );

                    // Send email (mocked)
                    var scopeName = className != null ? $"class {className}" : "general notices";
                    await _emailService.SendEmailAsync(
                        student.Email,
                        $"New Announcement from {teacherName}",
                        $"<h3>Hello {student.FirstName},</h3>" +
                        $"<p>{teacherName} has posted a new announcement under <strong>{scopeName}</strong>:</p>" +
                        $"<p><strong>Subject:</strong> {announcement.Title}</p>" +
                        $"<p>{announcement.Content}</p>" +
                        (string.IsNullOrEmpty(announcement.AttachmentUrl) ? "" : $"<p><strong>Attachment:</strong> <a href='{announcement.AttachmentUrl}'>View File</a></p>") +
                        $"<p>Log in to your dashboard to view all alerts.</p>"
                    );
                }
            }

            return new AnnouncementResponse(
                announcement.Id,
                announcement.Title,
                announcement.Content,
                announcement.AttachmentUrl,
                announcement.TeacherId,
                teacherName,
                announcement.ClassId,
                className,
                announcement.CreatedAt,
                IsRead: true // Posted by teacher, so not unread for the teacher
            );
        }

        public async Task<IEnumerable<AnnouncementResponse>> GetStudentAnnouncementsAsync(Guid studentId)
        {
            // Verify student exists
            var studentExists = await _context.Users.AnyAsync(u => u.Id == studentId);
            if (!studentExists) throw new KeyNotFoundException("Student not found.");

            // 1. Get class IDs student is enrolled in
            var enrolledClassIds = await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId)
                .Select(cs => cs.ClassId)
                .ToListAsync();

            // 2. Get teacher IDs student is mapped to
            var teacherIds = await _context.TeacherStudents
                .Where(ts => ts.StudentId == studentId)
                .Select(ts => ts.TeacherId)
                .ToListAsync();

            // Query announcements that target their classes OR global teacher alerts
            return await _context.Announcements
                .Where(a => (a.ClassId.HasValue && enrolledClassIds.Contains(a.ClassId.Value)) 
                            || (!a.ClassId.HasValue && teacherIds.Contains(a.TeacherId)))
                .Select(a => new
                {
                    Announcement = a,
                    Teacher = a.Teacher,
                    Class = a.Class,
                    IsRead = _context.AnnouncementReads.Any(ar => ar.AnnouncementId == a.Id && ar.StudentId == studentId)
                })
                .OrderByDescending(x => x.Announcement.CreatedAt)
                .Select(x => new AnnouncementResponse(
                    x.Announcement.Id,
                    x.Announcement.Title,
                    x.Announcement.Content,
                    x.Announcement.AttachmentUrl,
                    x.Announcement.TeacherId,
                    $"{x.Teacher.FirstName} {x.Teacher.LastName}",
                    x.Announcement.ClassId,
                    x.Class != null ? x.Class.Name : null,
                    x.Announcement.CreatedAt,
                    x.IsRead
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<AnnouncementResponse>> GetTeacherAnnouncementsAsync(Guid teacherId)
        {
            return await _context.Announcements
                .Where(a => a.TeacherId == teacherId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementResponse(
                    a.Id,
                    a.Title,
                    a.Content,
                    a.AttachmentUrl,
                    a.TeacherId,
                    $"{a.Teacher.FirstName} {a.Teacher.LastName}",
                    a.ClassId,
                    a.Class != null ? a.Class.Name : null,
                    a.CreatedAt,
                    true // For the posting teacher, it's always read
                ))
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid announcementId, Guid studentId)
        {
            // Verify announcement exists and is relevant to student
            var announcement = await _context.Announcements.FindAsync(announcementId);
            if (announcement == null) return false;

            var alreadyRead = await _context.AnnouncementReads
                .AnyAsync(ar => ar.AnnouncementId == announcementId && ar.StudentId == studentId);

            if (alreadyRead) return true;

            var read = new AnnouncementRead
            {
                AnnouncementId = announcementId,
                StudentId = studentId,
                ReadAt = DateTime.UtcNow
            };

            _context.AnnouncementReads.Add(read);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
