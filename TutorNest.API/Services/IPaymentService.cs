using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public interface IPaymentService
    {
        Task<CheckoutResponse> CreateCheckoutSessionAsync(CheckoutRequest request, Guid teacherId);
        Task<bool> ProcessStripeWebhookAsync(string json, string stripeSignatureHeader);
        Task<bool> ProcessPayHereWebhookAsync(IDictionary<string, string> formData);
    }
}
