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
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISubscriptionService _subscriptionService;

        public AuthController(IAuthService authService, ISubscriptionService subscriptionService)
        {
            _authService = authService;
            _subscriptionService = subscriptionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                if (response == null)
                {
                    return Unauthorized(new { message = "Invalid email or password." });
                }
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register-teacher")]
        [Authorize(Roles = ApplicationRole.Admin)]
        public async Task<IActionResult> RegisterTeacher([FromBody] RegisterTeacherRequest request)
        {
            try
            {
                var teacher = await _authService.RegisterTeacherAsync(request);
                return Ok(new { message = "Teacher registered successfully.", teacherId = teacher?.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("register-student")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterRequest request)
        {
            try
            {
                var teacherIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(teacherIdClaim) || !Guid.TryParse(teacherIdClaim, out var teacherId))
                {
                    return Unauthorized(new { message = "Invalid user identification." });
                }

                // Verify subscription student limits
                if (!await _subscriptionService.IsWithinStudentLimitAsync(teacherId))
                {
                    return StatusCode(403, new { message = "Student limit exceeded. Upgrade your subscription plan." });
                }

                var student = await _authService.RegisterStudentAsync(request, teacherId);
                return Ok(new { message = "Student registered successfully.", studentId = student?.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
