using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.Data;
using TutorNest.API.Entities;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ThemeController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly TutorNestDbContext _context;

        public ThemeController(ISubscriptionService subscriptionService, TutorNestDbContext context)
        {
            _subscriptionService = subscriptionService;
            _context = context;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentTheme()
        {
            try
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
                {
                    return Unauthorized(new { message = "Invalid user identity." });
                }

                var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? ApplicationRole.Student;

                string theme = "default";
                if (roleClaim == ApplicationRole.Admin)
                {
                    theme = "default";
                }
                else if (roleClaim == ApplicationRole.Teacher)
                {
                    var user = await _context.Users.FindAsync(userId);
                    theme = user?.Theme ?? "default";
                }
                else if (roleClaim == ApplicationRole.Student)
                {
                    try
                    {
                        var teacherId = await _subscriptionService.GetTeacherIdForUserAsync(userId, roleClaim);
                        var teacher = await _context.Users.FindAsync(teacherId);
                        theme = teacher?.Theme ?? "default";
                    }
                    catch
                    {
                        theme = "default";
                    }
                }

                return Ok(new { theme });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
