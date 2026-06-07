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
        private readonly IStorageService _storageService;
        private readonly INotificationService _notificationService;

        public TeacherController(
            ITeacherService teacherService, 
            ISubscriptionService subscriptionService,
            IStorageService storageService,
            INotificationService notificationService)
        {
            _teacherService = teacherService;
            _subscriptionService = subscriptionService;
            _storageService = storageService;
            _notificationService = notificationService;
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

        [HttpPost("videos/upload")]
        [RequestSizeLimit(500_000_000)] // 500 MB max for videos
        public async Task<IActionResult> UploadVideo(
            [FromForm] IFormFile file,
            [FromForm] string title,
            [FromForm] string description)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No video file uploaded." });

                var teacherId = GetTeacherId();
                var userId = teacherId;

                // Check storage plan quota
                var withinLimit = await _subscriptionService.IsWithinStorageLimitAsync(teacherId, file.Length);

                // Upload to Cloudflare R2
                var fileUrl = await _storageService.UploadAsync(file, "videos");

                // Track usage in database (updates subscription bytes and inserts UploadedFile)
                await _subscriptionService.TrackFileUploadAsync(teacherId, userId, file.FileName, fileUrl, file.Length);

                // Save Video entity
                var videoRequest = new CreateVideoRequest(title, description, fileUrl);
                var response = await _teacherService.CreateVideoAsync(videoRequest, teacherId);

                if (!withinLimit)
                {
                    // Create in-app system notification for the teacher to upgrade
                    await _notificationService.CreateNotificationAsync(
                        userId, 
                        $"Storage limit exceeded (used: {file.Length / (1024 * 1024)} MB for video: '{title}'). Please upgrade your subscription plan.", 
                        "System"
                    );
                }

                return Ok(new { 
                    video = response, 
                    limitExceeded = !withinLimit, 
                    message = !withinLimit ? "Storage limit exceeded. Please upgrade your subscription plan to avoid restrictions." : "Video uploaded and added to library successfully."
                });
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

        [HttpPut("classes/{classId:guid}")]
        public async Task<IActionResult> UpdateClass(Guid classId, [FromBody] CreateClassRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                var result = await _teacherService.UpdateClassAsync(classId, request, teacherId);
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

        [HttpDelete("classes/{classId:guid}")]
        public async Task<IActionResult> DeleteClass(Guid classId)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.DeleteClassAsync(classId, teacherId);
                return Ok(new { message = "Class deleted successfully." });
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

        [HttpPut("students/{studentId:guid}")]
        public async Task<IActionResult> UpdateStudent(Guid studentId, [FromBody] UpdateStudentRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                var result = await _teacherService.UpdateStudentAsync(studentId, request, teacherId);
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

        [HttpDelete("students/{studentId:guid}")]
        public async Task<IActionResult> DeleteStudent(Guid studentId)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.DeleteStudentAsync(studentId, teacherId);
                return Ok(new { message = "Student deleted successfully." });
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

        [HttpDelete("classes/{classId:guid}/students/{studentId:guid}")]
        public async Task<IActionResult> RemoveStudentFromClass(Guid classId, Guid studentId)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.RemoveStudentFromClassAsync(classId, studentId, teacherId);
                return Ok(new { message = "Student removed from class successfully." });
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

        [HttpPost("certificates")]
        public async Task<IActionResult> AwardCertificate([FromBody] AwardCertificateRequest request)
        {
            try
            {
                var teacherId = GetTeacherId();
                var result = await _teacherService.AwardCertificateAsync(request, teacherId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("certificates")]
        public async Task<IActionResult> GetTeacherCertificates()
        {
            try
            {
                var teacherId = GetTeacherId();
                var certificates = await _teacherService.GetTeacherCertificatesAsync(teacherId);
                return Ok(certificates);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("certificates/{certificateId:guid}")]
        public async Task<IActionResult> DeleteCertificate(Guid certificateId)
        {
            try
            {
                var teacherId = GetTeacherId();
                await _teacherService.DeleteCertificateAsync(certificateId, teacherId);
                return Ok(new { message = "Certificate deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
