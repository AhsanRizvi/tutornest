using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.DTOs;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated correctly.");
            }
            return userId;
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var response = await _announcementService.CreateAnnouncementAsync(request, teacherId);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("student")]
        [Authorize(Roles = ApplicationRole.Student)]
        public async Task<IActionResult> GetStudentAnnouncements()
        {
            try
            {
                var studentId = GetUserId();
                var announcements = await _announcementService.GetStudentAnnouncementsAsync(studentId);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("teacher")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> GetTeacherAnnouncements()
        {
            try
            {
                var teacherId = GetUserId();
                var announcements = await _announcementService.GetTeacherAnnouncementsAsync(teacherId);
                return Ok(announcements);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{announcementId:guid}/read")]
        [Authorize(Roles = ApplicationRole.Student)]
        public async Task<IActionResult> MarkAsRead(Guid announcementId)
        {
            try
            {
                var studentId = GetUserId();
                var success = await _announcementService.MarkAsReadAsync(announcementId, studentId);
                if (!success) return NotFound(new { message = "Announcement not found." });
                return Ok(new { message = "Announcement marked as read." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
