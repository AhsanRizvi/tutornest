using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = ApplicationRole.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers()
        {
            try
            {
                var teachers = await _adminService.GetTeachersAsync();
                return Ok(teachers);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("users/{id:guid}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id)
        {
            try
            {
                await _adminService.SuspendUserAsync(id);
                return Ok(new { message = "User account suspended successfully." });
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

        [HttpPost("users/{id:guid}/unsuspend")]
        public async Task<IActionResult> UnsuspendUser(Guid id)
        {
            try
            {
                await _adminService.UnsuspendUserAsync(id);
                return Ok(new { message = "User account unsuspended successfully." });
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

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            try
            {
                var plans = await _adminService.GetPlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("plans")]
        public async Task<IActionResult> CreatePlan([FromBody] DTOs.CreatePlanRequest request)
        {
            try
            {
                var result = await _adminService.CreatePlanAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("plans/{id:guid}")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] DTOs.CreatePlanRequest request)
        {
            try
            {
                var result = await _adminService.UpdatePlanAsync(id, request);
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

        [HttpGet("revenue-report")]
        public async Task<IActionResult> GetRevenueReport()
        {
            try
            {
                var result = await _adminService.GetRevenueReportAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("teachers/{id:guid}/theme")]
        public async Task<IActionResult> UpdateTeacherTheme(Guid id, [FromBody] DTOs.UpdateThemeRequest request)
        {
            try
            {
                await _adminService.UpdateTeacherThemeAsync(id, request.Theme);
                return Ok(new { message = "Theme updated successfully.", theme = request.Theme });
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

        [HttpGet("agora")]
        public async Task<IActionResult> GetAgoraSettings()
        {
            try
            {
                var result = await _adminService.GetAgoraSettingsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("agora")]
        public async Task<IActionResult> UpdateAgoraSettings([FromBody] DTOs.UpdateAgoraSettingsRequest request)
        {
            try
            {
                await _adminService.UpdateAgoraSettingsAsync(request);
                return Ok(new { message = "Agora settings updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
