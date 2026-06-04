using System;
using System.Collections.Generic;
using System.IO;
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
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ISubscriptionService _subscriptionService;

        public PaymentController(IPaymentService paymentService, ISubscriptionService subscriptionService)
        {
            _paymentService = paymentService;
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

        [HttpPost("checkout")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var response = await _paymentService.CreateCheckoutSessionAsync(request, teacherId);
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

        [HttpPost("mock-checkout")]
        [Authorize(Roles = ApplicationRole.Teacher)]
        public async Task<IActionResult> MockCheckout([FromBody] MockCheckoutRequest request)
        {
            try
            {
                var teacherId = GetUserId();
                var txId = $"mock_tx_{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
                await _subscriptionService.UpgradeSubscriptionAsync(teacherId, request.PlanId, "Mock", txId);
                return Ok(new { message = "Subscription upgraded successfully using Sandbox Simulation.", transactionId = txId });
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

        [HttpPost("stripe-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var json = await reader.ReadToEndAsync();
                var signature = Request.Headers["Stripe-Signature"].ToString();

                var success = await _paymentService.ProcessStripeWebhookAsync(json, signature);
                if (success) return Ok();
                return BadRequest(new { message = "Webhook parsing did not result in an upgrade." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("payhere-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayHereWebhook()
        {
            try
            {
                var dict = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    dict[key] = Request.Form[key].ToString();
                }

                var success = await _paymentService.ProcessPayHereWebhookAsync(dict);
                if (success) return Ok("Webhook processed");
                return BadRequest("Signature validation failed or payment incomplete");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public record MockCheckoutRequest(Guid PlanId);
}
