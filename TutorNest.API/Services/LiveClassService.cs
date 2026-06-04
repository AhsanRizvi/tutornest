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
    public class LiveClassService : ILiveClassService
    {
        private readonly TutorNestDbContext _context;

        public LiveClassService(TutorNestDbContext context)
        {
            _context = context;
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
    }
}
