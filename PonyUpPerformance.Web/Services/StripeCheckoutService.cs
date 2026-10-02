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

        public async Task<string> CreateCheckoutUrlAsync(
            ApplicationUser user,
            string planKey,
            string billingInterval,
            string baseUrl)
        {
            string secretKey =
                _config["Stripe:SecretKey"]
                ?? throw new InvalidOperationException(
                    "Stripe SecretKey missing.");

            StripeConfiguration.ApiKey =
                secretKey;

            string normalizedPlan =
                PonyUpPlanCatalog.NormalizeKey(
                    planKey);

            string normalizedBilling =
                NormalizeBillingInterval(
                    billingInterval);

            if (normalizedPlan ==
                PonyUpPlanCatalog.FreeKey)
            {
                throw new InvalidOperationException(
                    "The selected plan is not available for checkout.");
            }

            if (normalizedPlan is
                PonyUpPlanCatalog.RedlineKey or
                PonyUpPlanCatalog.RedlinePlusKey)
            {
                throw new InvalidOperationException(
                    "Redline checkout is not open until RevUp report pricing is finalized.");
            }

            string priceId =
                GetPriceId(
                    normalizedPlan,
                    normalizedBilling);

            string mode =
                normalizedPlan ==
                    PonyUpPlanCatalog.QuickPackKey
                    ? "payment"
                    : "subscription";

            var metadata =
                new Dictionary<string, string>
                {
                    { "UserId", user.Id },
                    { "PlanKey", normalizedPlan },
                    { "BillingInterval", normalizedBilling }
                };

            var options =
                new SessionCreateOptions
                {
                    Mode = mode,
                    SuccessUrl =
                        $"{baseUrl}/CheckoutSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl =
                        $"{baseUrl}/Pricing",
                    Customer =
                        string.IsNullOrWhiteSpace(
                            user.StripeCustomerId)
                            ? null
                            : user.StripeCustomerId,
                    CustomerEmail =
                        string.IsNullOrWhiteSpace(
                            user.StripeCustomerId)
                            ? user.Email
                            : null,
                    ClientReferenceId =
                        user.Id,
                    Metadata =
                        metadata,
                    SubscriptionData =
                        mode == "subscription"
                            ? new SessionSubscriptionDataOptions
                            {
                                Metadata = metadata
                            }
                            : null,
                    LineItems =
                        new List<SessionLineItemOptions>
                        {
                            new()
                            {
                                Price = priceId,
                                Quantity = 1
                            }
                        }
                };

            var service =
                new SessionService();

            Session session =
                await service.CreateAsync(
                    options);

            return session.Url;
        }

        public async Task<string> CreateCustomerPortalUrlAsync(
            ApplicationUser user,
            string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    user.StripeCustomerId))
            {
                throw new InvalidOperationException(
                    "No Stripe customer is attached to this PonyUp account.");
            }

            string secretKey =
                _config["Stripe:SecretKey"]
                ?? throw new InvalidOperationException(
                    "Stripe SecretKey missing.");

            StripeConfiguration.ApiKey =
                secretKey;

            var options =
                new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer =
                        user.StripeCustomerId,

                    ReturnUrl =
                        returnUrl
                };

            var service =
                new Stripe.BillingPortal.SessionService();

            Stripe.BillingPortal.Session session =
                await service.CreateAsync(
                    options);

            return session.Url;
        }

        public async Task<Session> GetSessionAsync(
            string sessionId)
        {
            string secretKey =
                _config["Stripe:SecretKey"]
                ?? throw new InvalidOperationException(
                    "Stripe SecretKey missing.");

            StripeConfiguration.ApiKey =
                secretKey;

            var service =
                new SessionService();

            return await service.GetAsync(
                sessionId);
        }

        public string ResolvePlanKeyFromPriceId(
            string? priceId)
        {
            if (string.IsNullOrWhiteSpace(
                    priceId))
            {
                return PonyUpPlanCatalog.FreeKey;
            }

            if (MatchesConfiguredPrice(
                    priceId,
                    "Stripe:QuickPackPriceId"))
            {
                return PonyUpPlanCatalog.QuickPackKey;
            }

            if (MatchesConfiguredPrice(
                    priceId,
                    "Stripe:ProMonthlyPriceId",
                    "Stripe:ProPriceId",
                    "Stripe:ProAnnualPriceId"))
            {
                return PonyUpPlanCatalog.ProKey;
            }

            if (MatchesConfiguredPrice(
                    priceId,
                    "Stripe:FullThrottleMonthlyPriceId",
                    "Stripe:UnlimitedPriceId",
                    "Stripe:FullThrottleAnnualPriceId"))
            {
                return PonyUpPlanCatalog.FullThrottleKey;
            }

            return PonyUpPlanCatalog.FreeKey;
        }

        private bool MatchesConfiguredPrice(
            string priceId,
            params string[] keys)
        {
            foreach (string key in keys)
            {
                string? configured =
                    _config[key];

                if (!string.IsNullOrWhiteSpace(
                        configured) &&
                    string.Equals(
                        configured,
                        priceId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetPriceId(
            string planKey,
            string billingInterval)
        {
            if (planKey ==
                PonyUpPlanCatalog.QuickPackKey)
            {
                return RequiredConfig(
                    "Stripe:QuickPackPriceId");
            }

            if (planKey ==
                PonyUpPlanCatalog.ProKey)
            {
                return billingInterval == "annual"
                    ? RequiredConfig(
                        "Stripe:ProAnnualPriceId")
                    : FirstConfigured(
                        "Stripe:ProMonthlyPriceId",
                        "Stripe:ProPriceId");
            }

            if (planKey ==
                PonyUpPlanCatalog.FullThrottleKey)
            {
                return billingInterval == "annual"
                    ? RequiredConfig(
                        "Stripe:FullThrottleAnnualPriceId")
                    : FirstConfigured(
                        "Stripe:FullThrottleMonthlyPriceId",
                        "Stripe:UnlimitedPriceId");
            }

            throw new InvalidOperationException(
                "Invalid or unavailable PonyUp plan.");
        }

        private string RequiredConfig(
            string key)
        {
            string? value =
                _config[key];

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                throw new InvalidOperationException(
                    $"{key} is not configured.");
            }

            return value;
        }

        private string FirstConfigured(
            params string[] keys)
        {
            foreach (string key in keys)
            {
                string? value =
                    _config[key];

                if (!string.IsNullOrWhiteSpace(
                        value))
                {
                    return value;
                }
            }

            throw new InvalidOperationException(
                $"Stripe price is not configured. Checked: {string.Join(", ", keys)}.");
        }

        private static string NormalizeBillingInterval(
            string? billingInterval)
        {
            return string.Equals(
                    billingInterval,
                    "annual",
                    StringComparison.OrdinalIgnoreCase)
                ? "annual"
                : "monthly";
        }
    }
}
