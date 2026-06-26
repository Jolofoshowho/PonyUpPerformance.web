using Microsoft.AspNetCore.Identity;
using PonyUpPerformance.Web.Models;
using Stripe;
using Stripe.Checkout;

namespace PonyUpPerformance.Web.Services
{
    public class StripeCheckoutService
    {
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;

        public StripeCheckoutService(
            IConfiguration config,
            UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public async Task<string> CreateCheckoutUrlAsync(ApplicationUser user, string planKey, string baseUrl)
        {
            string secretKey = _config["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey missing.");
            StripeConfiguration.ApiKey = secretKey;

            string priceId = GetPriceId(planKey);
            string mode = planKey == "quickpack" ? "payment" : "subscription";

            var options = new SessionCreateOptions
            {
                Mode = mode,
                SuccessUrl = $"{baseUrl}/CheckoutSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{baseUrl}/#pricing",
                CustomerEmail = user.Email,
                ClientReferenceId = user.Id,
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", user.Id },
                    { "PlanKey", planKey }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                }
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task<Session> GetSessionAsync(string sessionId)
        {
            string secretKey = _config["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey missing.");
            StripeConfiguration.ApiKey = secretKey;

            var service = new SessionService();
            return await service.GetAsync(sessionId);
        }

        private string GetPriceId(string planKey)
        {
            return planKey.ToLowerInvariant() switch
            {
                "quickpack" => _config["Stripe:QuickPackPriceId"] ?? throw new InvalidOperationException("QuickPack price missing."),
                "pro" => _config["Stripe:ProPriceId"] ?? throw new InvalidOperationException("Pro price missing."),
                "unlimited" => _config["Stripe:UnlimitedPriceId"] ?? throw new InvalidOperationException("Unlimited price missing."),
                _ => throw new InvalidOperationException("Invalid plan.")
            };
        }
    }
}