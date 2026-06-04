using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TutorNest.API.Data;
using TutorNest.API.DTOs;

namespace TutorNest.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly TutorNestDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;

        public PaymentService(
            TutorNestDbContext context,
            ISubscriptionService subscriptionService,
            IConfiguration config,
            ILogger<PaymentService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _config = config;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<CheckoutResponse> CreateCheckoutSessionAsync(CheckoutRequest request, Guid teacherId)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
            if (plan == null) throw new KeyNotFoundException("Pricing plan not found.");

            var useMock = _config.GetValue<bool>("Payment:UseMockSandbox", true);
            var stripeKey = _config["Payment:StripeSecretKey"];
            var payHereMerchantId = _config["Payment:PayHereMerchantId"];

            // 1. Fallback Mock Checkout Session
            if (useMock || (string.IsNullOrEmpty(stripeKey) && string.IsNullOrEmpty(payHereMerchantId)))
            {
                var mockTxId = $"mock_tx_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
                // Redirect user to the mock checkout page built into our Angular client
                var mockUrl = $"http://localhost:4200/billing?mockSessionId={mockTxId}&planId={plan.Id}";
                return new CheckoutResponse(mockUrl, true);
            }

            // 2. Stripe Payment Checkout Session
            if (!string.IsNullOrEmpty(stripeKey))
            {
                try
                {
                    var stripeUrl = "https://api.stripe.com/v1/checkout/sessions";
                    var clientReference = $"{teacherId}|{plan.Id}";

                    var requestData = new List<KeyValuePair<string, string>>
                    {
                        new("success_url", request.SuccessUrl + "?sessionId={CHECKOUT_SESSION_ID}"),
                        new("cancel_url", request.CancelUrl),
                        new("mode", "payment"),
                        new("client_reference_id", clientReference),
                        new("line_items[0][price_data][currency]", plan.Currency.ToLower()),
                        new("line_items[0][price_data][product_data][name]", $"TutorNest {plan.Name} Plan"),
                        new("line_items[0][price_data][unit_amount]", ((long)(plan.Price * 100)).ToString()),
                        new("line_items[0][quantity]", "1")
                    };

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, stripeUrl);
                    httpRequest.Content = new FormUrlEncodedContent(requestData);
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", stripeKey);

                    var httpResponse = await _httpClient.SendAsync(httpRequest);
                    var responseJson = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Stripe session creation failed: {Response}", responseJson);
                        throw new InvalidOperationException("Failed to initiate Stripe gateway session.");
                    }

                    using var doc = JsonDocument.Parse(responseJson);
                    var sessionUrl = doc.RootElement.GetProperty("url").GetString();
                    
                    return new CheckoutResponse(sessionUrl ?? request.SuccessUrl, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe Checkout session initiation failed.");
                }
            }

            // 3. PayHere Checkout Parameters Redirect Generator
            if (!string.IsNullOrEmpty(payHereMerchantId))
            {
                try
                {
                    var payhereSecret = _config["Payment:PayHereSecret"] ?? "PayHereSecretSandboxKey";
                    var orderId = $"TN_{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()}";
                    var amountVal = plan.Price;
                    var currencyVal = plan.Currency; // LKR default recommended for PayHere

                    // MD5 encryption hash formula
                    // Hash = Upper(MD5(merchant_id + order_id + amount_formatted + currency + Upper(MD5(merchant_secret))))
                    var secretHashUpper = GetMd5Hash(payhereSecret).ToUpper();
                    var rawSignature = $"{payHereMerchantId}{orderId}{amountVal:0.00}{currencyVal}{secretHashUpper}";
                    var signatureHash = GetMd5Hash(rawSignature).ToUpper();

                    var isSandbox = _config.GetValue<bool>("Payment:PayHereSandboxMode", true);
                    var gatewayDomain = isSandbox ? "sandbox.payhere.lk" : "www.payhere.lk";
                    
                    var notifyUrl = "http://localhost:5299/api/payment/payhere-webhook";

                    var queryUrl = $"https://{gatewayDomain}/pay/checkout" +
                                   $"?merchant_id={payHereMerchantId}" +
                                   $"&order_id={orderId}" +
                                   $"&items={Uri.EscapeDataString($"TutorNest {plan.Name} Plan")}" +
                                   $"&amount={amountVal:0.00}" +
                                   $"&currency={currencyVal}" +
                                   $"&hash={signatureHash}" +
                                   $"&return_url={Uri.EscapeDataString(request.SuccessUrl)}" +
                                   $"&cancel_url={Uri.EscapeDataString(request.CancelUrl)}" +
                                   $"&notify_url={Uri.EscapeDataString(notifyUrl)}" +
                                   $"&custom_1={teacherId}" +
                                   $"&custom_2={plan.Id}";

                    return new CheckoutResponse(queryUrl, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PayHere link generation failed.");
                }
            }

            throw new InvalidOperationException("No payment gateway provider configured or available.");
        }

        public async Task<bool> ProcessStripeWebhookAsync(string json, string stripeSignatureHeader)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var eventType = root.GetProperty("type").GetString();

                if (eventType == "checkout.session.completed")
                {
                    var dataObject = root.GetProperty("data").GetProperty("object");
                    var clientRef = dataObject.GetProperty("client_reference_id").GetString();
                    var transactionId = dataObject.GetProperty("id").GetString();

                    if (!string.IsNullOrEmpty(clientRef) && clientRef.Contains("|"))
                    {
                        var parts = clientRef.Split('|');
                        if (Guid.TryParse(parts[0], out var teacherId) && Guid.TryParse(parts[1], out var planId))
                        {
                            var externalSubId = dataObject.TryGetProperty("subscription", out var subProp) ? subProp.GetString() : null;
                            await _subscriptionService.UpgradeSubscriptionAsync(teacherId, planId, "Stripe", transactionId!, externalSubId);
                            _logger.LogInformation("Successfully upgraded teacher {TeacherId} to plan {PlanId} via Stripe Webhook.", teacherId, planId);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook evaluation exception occurred.");
            }

            return false;
        }

        public async Task<bool> ProcessPayHereWebhookAsync(IDictionary<string, string> formData)
        {
            try
            {
                var merchantId = formData["merchant_id"];
                var orderId = formData["order_id"];
                var paymentId = formData["payment_id"];
                var payhereAmount = formData["payhere_amount"];
                var payhereCurrency = formData["payhere_currency"];
                var statusCode = formData["status_code"];
                var requestHash = formData["md5sig"];

                var payhereSecret = _config["Payment:PayHereSecret"] ?? "PayHereSecretSandboxKey";

                // Generate signature for verification
                var secretHashUpper = GetMd5Hash(payhereSecret).ToUpper();
                var rawSignature = $"{merchantId}{orderId}{payhereAmount}{payhereCurrency}{statusCode}{secretHashUpper}";
                var generatedHash = GetMd5Hash(rawSignature).ToUpper();

                if (requestHash != generatedHash)
                {
                    _logger.LogWarning("PayHere webhook signature validation failed. Expected: {Exp}, Got: {Got}", generatedHash, requestHash);
                    return false;
                }

                if (statusCode == "2") // 2 means success / payment completed
                {
                    var teacherIdStr = formData.ContainsKey("custom_1") ? formData["custom_1"] : null;
                    var planIdStr = formData.ContainsKey("custom_2") ? formData["custom_2"] : null;

                    if (Guid.TryParse(teacherIdStr, out var teacherId) && Guid.TryParse(planIdStr, out var planId))
                    {
                        await _subscriptionService.UpgradeSubscriptionAsync(teacherId, planId, "PayHere", paymentId);
                        _logger.LogInformation("Successfully upgraded teacher {TeacherId} to plan {PlanId} via PayHere Webhook.", teacherId, planId);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayHere webhook verification failed.");
            }

            return false;
        }

        private static string GetMd5Hash(string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
