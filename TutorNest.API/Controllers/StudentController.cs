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
    [Authorize(Roles = ApplicationRole.Student)]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        private Guid GetStudentId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var studentId))
            {
                throw new UnauthorizedAccessException("Student is not authenticated correctly.");
            }
            return studentId;
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetMyClasses()
        {
            try
            {
                var studentId = GetStudentId();
                var classes = await _studentService.GetMyClassesAsync(studentId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("classes/{classId:guid}/videos")]
        public async Task<IActionResult> GetClassVideos(Guid classId)
        {
            try
            {
                var studentId = GetStudentId();
                var videos = await _studentService.GetClassVideosAsync(classId, studentId);
                return Ok(videos);
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

        [HttpPost("videos/{videoId:guid}/progress")]
        public async Task<IActionResult> UpdateProgress(Guid videoId, [FromBody] UpdateProgressRequest request)
        {
            try
            {
                var studentId = GetStudentId();
                var response = await _studentService.UpdateProgressAsync(videoId, studentId, request);
                return Ok(response);
            }
            catch (InvalidOperationException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("classes/{classId:guid}/leaderboard")]
        public async Task<IActionResult> GetClassLeaderboard(Guid classId)
        {
            try
            {
                var studentId = GetStudentId();
                var leaderboard = await _studentService.GetClassLeaderboardAsync(classId, studentId);
                return Ok(leaderboard);
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
