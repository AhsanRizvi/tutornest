using System;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class LiveClassController : ControllerBase
    {
        private readonly ILiveClassService _liveClassService;

        public LiveClassController(ILiveClassService liveClassService)
        {
            _liveClassService = liveClassService;
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

        private string GetUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Student";
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> ScheduleLiveClass([FromBody] CreateLiveClassRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var result = await _liveClassService.ScheduleLiveClassAsync(request, teacherId);
                return Ok(result);
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

        [HttpGet("class/{classId:guid}")]
        public async Task<IActionResult> GetClassLiveClasses(Guid classId)
        {
            try
            {
                var result = await _liveClassService.GetClassLiveClassesAsync(classId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingLiveClasses()
        {
            try
            {
                var userId = GetUserId();
                var role = GetUserRole();

                if (role == ApplicationRole.Teacher)
                {
                    var result = await _liveClassService.GetTeacherUpcomingLiveClassesAsync(userId);
                    return Ok(result);
                }
                else
                {
                    var result = await _liveClassService.GetStudentUpcomingLiveClassesAsync(userId);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/recording")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> UploadRecording(Guid id, [FromBody] UploadRecordingRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                await _liveClassService.SaveRecordingUrlAsync(id, request.RecordingUrl, teacherId);
                return Ok(new { message = "Recording link updated successfully." });
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
    }
}
