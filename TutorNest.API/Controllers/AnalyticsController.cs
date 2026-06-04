using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
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

        [HttpGet("teacher")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> GetTeacherAnalytics()
        {
            try
            {
                var teacherId = GetUserId();
                var analytics = await _analyticsService.GetTeacherAnalyticsAsync(teacherId);
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("admin")]
        [Authorize(Roles = ApplicationRole.Admin)]
        public async Task<IActionResult> GetAdminAnalytics()
        {
            try
            {
                var analytics = await _analyticsService.GetAdminAnalyticsAsync();
                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
