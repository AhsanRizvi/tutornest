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
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
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

        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _subscriptionService.GetActivePlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-status")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> GetMyStatus()
        {
            try
            {
                var teacherId = GetUserId();
                var status = await _subscriptionService.GetTeacherSubscriptionAsync(teacherId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("billing-history")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> GetBillingHistory()
        {
            try
            {
                var teacherId = GetUserId();
                var history = await _subscriptionService.GetPaymentHistoryAsync(teacherId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserId();
                var profile = await _subscriptionService.GetUserProfileAsync(userId);
                return Ok(profile);
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

        [HttpPost("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _subscriptionService.UpdateUserProfileAsync(userId, request);
                return Ok(new { message = "Profile updated successfully." });
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

        [HttpPost("admin/upgrade-teacher")]
        [Authorize(Roles = ApplicationRole.Admin)]
        public async Task<IActionResult> AdminUpgradeTeacher([FromBody] AdminUpgradeRequest request)
        {
            try
            {
                var txId = $"admin_override_{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";
                await _subscriptionService.UpgradeSubscriptionAsync(request.TeacherId, request.PlanId, "Admin", txId);
                return Ok(new { message = "Teacher plan upgraded successfully by Administrator." });
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

    public record AdminUpgradeRequest(Guid TeacherId, Guid PlanId);
}
