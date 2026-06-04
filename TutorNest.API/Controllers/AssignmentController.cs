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
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IStorageService _storageService;
        private readonly ISubscriptionService _subscriptionService;

        public AssignmentController(
            IAssignmentService assignmentService,
            IStorageService storageService,
            ISubscriptionService subscriptionService)
        {
            _assignmentService = assignmentService;
            _storageService = storageService;
            _subscriptionService = subscriptionService;
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
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            return roleClaim ?? ApplicationRole.Student;
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var response = await _assignmentService.CreateAssignmentAsync(request, teacherId);
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

        [HttpGet("class/{classId:guid}")]
        public async Task<IActionResult> GetClassAssignments(Guid classId)
        {
            try
            {
                var userId = GetUserId();
                var role = GetUserRole();
                var assignments = await _assignmentService.GetClassAssignmentsAsync(classId, userId, role);
                return Ok(assignments);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{assignmentId:guid}/submit")]
        [Authorize(Roles = ApplicationRole.Student)]
        public async Task<IActionResult> Submit(Guid assignmentId, [FromBody] SubmitAssignmentRequest request)
        {
            try
            {
                var studentId = GetUserId();
                var response = await _assignmentService.SubmitAssignmentAsync(assignmentId, studentId, request);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{assignmentId:guid}/submissions")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> GetSubmissions(Guid assignmentId)
        {
            try
            {
                var teacherId = GetUserId();
                var submissions = await _assignmentService.GetAssignmentSubmissionsAsync(assignmentId, teacherId);
                return Ok(submissions);
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

        [HttpPost("submission/{submissionId:guid}/grade")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> Grade(Guid submissionId, [FromBody] GradeSubmissionRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var response = await _assignmentService.GradeSubmissionAsync(submissionId, request, teacherId);
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

        [HttpGet("my-submissions")]
        [Authorize(Roles = ApplicationRole.Student)]
        public async Task<IActionResult> GetMySubmissions()
        {
            try
            {
                var studentId = GetUserId();
                var submissions = await _assignmentService.GetStudentSubmissionsAsync(studentId);
                return Ok(submissions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload")]
        [RequestSizeLimit(500_000_000)] // 500 MB max
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "No file uploaded." });

                // Enforce storage plan quota
                var userId = GetUserId();
                var role = GetUserRole();
                var teacherId = await _subscriptionService.GetTeacherIdForUserAsync(userId, role);

                if (!await _subscriptionService.IsWithinStorageLimitAsync(teacherId, file.Length))
                    return StatusCode(403, new { message = "Storage limit exceeded. Upgrade your subscription plan." });

                // Upload to Cloudflare R2 (or local fallback)
                var fileUrl = await _storageService.UploadAsync(file, "uploads");

                // Track usage in database
                await _subscriptionService.TrackFileUploadAsync(teacherId, userId, file.FileName, fileUrl, file.Length);

                return Ok(new { url = fileUrl });
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
