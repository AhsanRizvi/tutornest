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
    [Authorize(Roles = ApplicationRole.Teacher)]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherService _teacherService;
        private readonly ISubscriptionService _subscriptionService;

        public TeacherController(ITeacherService teacherService, ISubscriptionService subscriptionService)
        {
            _teacherService = teacherService;
            _subscriptionService = subscriptionService;
        }

        private Guid GetTeacherId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var teacherId))
            {
                throw new UnauthorizedAccessException("Teacher is not authenticated correctly.");
            }
            return teacherId;
        }

        [HttpPost("classes")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();

                // Enforce plan class limit
                if (!await _subscriptionService.IsWithinClassLimitAsync(teacherId))
                {
                    return StatusCode(403, new { message = "Class limit exceeded. Upgrade your subscription plan." });
                }

                var response = await _teacherService.CreateClassAsync(request, teacherId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var teacherId = GetTeacherId();
                var classes = await _teacherService.GetClassesAsync(teacherId);
                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("classes/{classId:guid}/enroll")]
        public async Task<IActionResult> EnrollStudent(Guid classId, [FromBody] EnrollStudentRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.EnrollStudentAsync(classId, request.StudentId, teacherId);
                return Ok(new { message = "Student enrolled successfully in class." });
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

        [HttpGet("classes/{classId:guid}/students")]
        public async Task<IActionResult> GetClassStudents(Guid classId)
        {
            try
            {
                var teacherId = GetTeacherId();
                var students = await _teacherService.GetClassStudentsAsync(classId, teacherId);
                return Ok(students);
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

        [HttpGet("students")]
        public async Task<IActionResult> GetMyStudents()
        {
            try
            {
                var teacherId = GetTeacherId();
                var students = await _teacherService.GetMyStudentsAsync(teacherId);
                return Ok(students);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("videos")]
        public async Task<IActionResult> CreateVideo([FromBody] CreateVideoRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                var response = await _teacherService.CreateVideoAsync(request, teacherId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("videos")]
        public async Task<IActionResult> GetMyVideos()
        {
            try
            {
                var teacherId = GetTeacherId();
                var videos = await _teacherService.GetMyVideosAsync(teacherId);
                return Ok(videos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("classes/{classId:guid}/videos")]
        public async Task<IActionResult> AssignVideo(Guid classId, [FromBody] AssignVideoRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.AssignVideoToClassAsync(classId, request.VideoId, teacherId);
                return Ok(new { message = "Video assigned to class successfully." });
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

        [HttpGet("classes/{classId:guid}/videos")]
        public async Task<IActionResult> GetClassVideos(Guid classId)
        {
            try
            {
                var teacherId = GetTeacherId();
                var videos = await _teacherService.GetClassVideosAsync(classId, teacherId);
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

        [HttpGet("progress")]
        public async Task<IActionResult> GetStudentsProgress()
        {
            try
            {
                var teacherId = GetTeacherId();
                var progress = await _teacherService.GetStudentsProgressAsync(teacherId);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
