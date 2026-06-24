using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AgoraIO.Rtc;
using TutorNest.API.Data;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;

namespace TutorNest.API.Services
{
    public class LiveClassService : ILiveClassService
    {
        private readonly TutorNestDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public LiveClassService(
            TutorNestDbContext context, 
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _context = context;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task<LiveClassResponse> ScheduleLiveClassAsync(CreateLiveClassRequest request, Guid teacherId)
        {
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class group not found or you do not have permission.");
            }

            var liveClass = new LiveClass
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                ScheduledStartTime = request.ScheduledStartTime.ToUniversalTime(),
                DurationMinutes = request.DurationMinutes,
                MeetingLink = request.MeetingLink,
                ClassId = request.ClassId,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LiveClasses.Add(liveClass);
            await _context.SaveChangesAsync();

            var teacher = await _context.Users.FirstAsync(u => u.Id == teacherId);

            return new LiveClassResponse(
                liveClass.Id,
                liveClass.Title,
                liveClass.Description,
                liveClass.ScheduledStartTime,
                liveClass.DurationMinutes,
                liveClass.MeetingLink,
                liveClass.RecordingUrl,
                liveClass.ClassId,
                @class.Name,
                liveClass.TeacherId,
                $"{teacher.FirstName} {teacher.LastName}",
                liveClass.CreatedAt
            );
        }

        public async Task<IEnumerable<LiveClassResponse>> GetClassLiveClassesAsync(Guid classId)
        {
            return await _context.LiveClasses
                .Where(lc => lc.ClassId == classId)
                .Include(lc => lc.Class)
                .Include(lc => lc.Teacher)
                .OrderBy(lc => lc.ScheduledStartTime)
                .Select(lc => new LiveClassResponse(
                    lc.Id,
                    lc.Title,
                    lc.Description,
                    lc.ScheduledStartTime,
                    lc.DurationMinutes,
                    lc.MeetingLink,
                    lc.RecordingUrl,
                    lc.ClassId,
                    lc.Class.Name,
                    lc.TeacherId,
                    $"{lc.Teacher.FirstName} {lc.Teacher.LastName}",
                    lc.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<LiveClassResponse>> GetTeacherUpcomingLiveClassesAsync(Guid teacherId)
        {
            var cutoff = DateTime.UtcNow.AddHours(-2); // Show classes scheduled up to 2 hours ago
            return await _context.LiveClasses
                .Where(lc => lc.TeacherId == teacherId && lc.ScheduledStartTime >= cutoff)
                .Include(lc => lc.Class)
                .Include(lc => lc.Teacher)
                .OrderBy(lc => lc.ScheduledStartTime)
                .Select(lc => new LiveClassResponse(
                    lc.Id,
                    lc.Title,
                    lc.Description,
                    lc.ScheduledStartTime,
                    lc.DurationMinutes,
                    lc.MeetingLink,
                    lc.RecordingUrl,
                    lc.ClassId,
                    lc.Class.Name,
                    lc.TeacherId,
                    $"{lc.Teacher.FirstName} {lc.Teacher.LastName}",
                    lc.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<LiveClassResponse>> GetStudentUpcomingLiveClassesAsync(Guid studentId)
        {
            var enrolledClassIds = await _context.ClassStudents
                .Where(cs => cs.StudentId == studentId)
                .Select(cs => cs.ClassId)
                .ToListAsync();

            var cutoff = DateTime.UtcNow.AddHours(-2);
            return await _context.LiveClasses
                .Where(lc => enrolledClassIds.Contains(lc.ClassId) && lc.ScheduledStartTime >= cutoff)
                .Include(lc => lc.Class)
                .Include(lc => lc.Teacher)
                .OrderBy(lc => lc.ScheduledStartTime)
                .Select(lc => new LiveClassResponse(
                    lc.Id,
                    lc.Title,
                    lc.Description,
                    lc.ScheduledStartTime,
                    lc.DurationMinutes,
                    lc.MeetingLink,
                    lc.RecordingUrl,
                    lc.ClassId,
                    lc.Class.Name,
                    lc.TeacherId,
                    $"{lc.Teacher.FirstName} {lc.Teacher.LastName}",
                    lc.CreatedAt
                ))
                .ToListAsync();
        }

        public async Task SaveRecordingUrlAsync(Guid liveClassId, string recordingUrl, Guid teacherId)
        {
            var liveClass = await _context.LiveClasses
                .FirstOrDefaultAsync(lc => lc.Id == liveClassId && lc.TeacherId == teacherId);
            if (liveClass == null)
            {
                throw new KeyNotFoundException("Live class session not found or you do not have permission.");
            }

            liveClass.RecordingUrl = recordingUrl;
            await _context.SaveChangesAsync();
        }

        public async Task<LiveClassResponse> GetLiveClassByIdAsync(Guid id)
        {
            var lc = await _context.LiveClasses
                .Include(x => x.Class)
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (lc == null)
            {
                throw new KeyNotFoundException("Live class session not found.");
            }

            return new LiveClassResponse(
                lc.Id,
                lc.Title,
                lc.Description,
                lc.ScheduledStartTime,
                lc.DurationMinutes,
                lc.MeetingLink,
                lc.RecordingUrl,
                lc.ClassId,
                lc.Class.Name,
                lc.TeacherId,
                $"{lc.Teacher.FirstName} {lc.Teacher.LastName}",
                lc.CreatedAt
            );
        }

        public async Task<LiveClassResponse> StartInstantLiveClassAsync(CreateLiveClassRequest request, Guid teacherId)
        {
            var @class = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId && c.TeacherId == teacherId);
            if (@class == null)
            {
                throw new KeyNotFoundException("Class group not found or you do not have permission.");
            }

            var liveClassId = Guid.NewGuid();
            var liveClass = new LiveClass
            {
                Id = liveClassId,
                Title = request.Title,
                Description = request.Description,
                ScheduledStartTime = DateTime.UtcNow,
                DurationMinutes = request.DurationMinutes,
                MeetingLink = $"/live-class/{liveClassId}",
                ClassId = request.ClassId,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LiveClasses.Add(liveClass);
            await _context.SaveChangesAsync();

            // Fetch students enrolled in this classroom to send notifications
            var studentIds = await _context.ClassStudents
                .Where(cs => cs.ClassId == request.ClassId)
                .Select(cs => cs.StudentId)
                .ToListAsync();

            foreach (var studentId in studentIds)
            {
                await _notificationService.CreateNotificationAsync(
                    studentId,
                    $"Live Class '{request.Title}' has started for {@class.Name}. Click here to join Agora Room!",
                    "LiveClass"
                );
            }

            var teacher = await _context.Users.FirstAsync(u => u.Id == teacherId);

            return new LiveClassResponse(
                liveClass.Id,
                liveClass.Title,
                liveClass.Description,
                liveClass.ScheduledStartTime,
                liveClass.DurationMinutes,
                liveClass.MeetingLink,
                liveClass.RecordingUrl,
                liveClass.ClassId,
                @class.Name,
                liveClass.TeacherId,
                $"{teacher.FirstName} {teacher.LastName}",
                liveClass.CreatedAt
            );
        }

        public async Task<AgoraTokenResponse> GetAgoraTokenAsync(Guid liveClassId, Guid userId, string role)
        {
            var liveClass = await _context.LiveClasses
                .Include(lc => lc.Class)
                .FirstOrDefaultAsync(lc => lc.Id == liveClassId);

            if (liveClass == null)
            {
                throw new KeyNotFoundException("Live class session not found.");
            }

            if (role == ApplicationRole.Teacher)
            {
                if (liveClass.TeacherId != userId)
                {
                    throw new UnauthorizedAccessException("You are not the teacher for this live class.");
                }
            }
            else
            {
                var isEnrolled = await _context.ClassStudents
                    .AnyAsync(cs => cs.ClassId == liveClass.ClassId && cs.StudentId == userId);
                if (!isEnrolled)
                {
                    throw new UnauthorizedAccessException("You are not enrolled in the classroom for this live class.");
                }
            }

            var appId = _configuration["Agora:AppId"] ?? string.Empty;
            var appCertificate = _configuration["Agora:AppCertificate"] ?? string.Empty;

            if (appId == "YOUR_AGORA_APP_ID") appId = string.Empty;
            if (appCertificate == "YOUR_AGORA_APP_CERTIFICATE") appCertificate = string.Empty;

            string channelName = liveClassId.ToString();
            byte[] guidBytes = userId.ToByteArray();
            uint uid = (uint)(BitConverter.ToUInt32(guidBytes, 0) ^ BitConverter.ToUInt32(guidBytes, 8));

            string token = string.Empty;

            if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(appCertificate))
            {
                try
                {
                    var builder = new RtcTokenBuilder();
                    uint privilegeExpiration = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 7200); // 2 hours
                    
                    bool isPublisher = true; // both teachers and students can publish audio/video in the interactive class
                    token = builder.BuildToken(appId, appCertificate, channelName, isPublisher, privilegeExpiration);
                }
                catch
                {
                    token = string.Empty;
                }
            }

            byte[] teacherGuidBytes = liveClass.TeacherId.ToByteArray();
            uint teacherUid = (uint)(BitConverter.ToUInt32(teacherGuidBytes, 0) ^ BitConverter.ToUInt32(teacherGuidBytes, 8));

            return new AgoraTokenResponse(token, appId, channelName, uid, teacherUid);
        }
    }
}
