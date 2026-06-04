using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorNest.API.Services;

namespace TutorNest.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
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

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetNotificationsAsync(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{notificationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _notificationService.MarkAsReadAsync(notificationId, userId);
                if (!success) return NotFound(new { message = "Notification not found." });
                return Ok(new { message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();
                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok(new { message = "All notifications marked as read." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
