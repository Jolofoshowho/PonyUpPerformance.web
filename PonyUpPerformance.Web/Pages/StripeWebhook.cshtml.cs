using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using Stripe;

namespace PonyUpPerformance.Web.Pages
{
    [IgnoreAntiforgeryToken]
    public class StripeWebhookModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly PlanEntitlementService _planEntitlementService;
        private readonly StripeCheckoutService _stripeCheckoutService;

        public StripeWebhookModel(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            PlanEntitlementService planEntitlementService,
            StripeCheckoutService stripeCheckoutService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _planEntitlementService = planEntitlementService;
            _stripeCheckoutService = stripeCheckoutService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string webhookSecret =
                _configuration["Stripe:WebhookSecret"]
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    webhookSecret))
            {
                return new StatusCodeResult(
                    StatusCodes.Status503ServiceUnavailable);
            }

            string payload;

            using (var reader =
                   new StreamReader(
                       Request.Body))
            {
                payload =
                    await reader.ReadToEndAsync();
            }

            string signature =
                Request.Headers["Stripe-Signature"]
                    .ToString();

            Event stripeEvent;

            try
            {
                stripeEvent =
                    EventUtility.ConstructEvent(
                        payload,
                        signature,
                        webhookSecret);
            }
            catch
            {
                return new BadRequestResult();
            }

            bool alreadyProcessed =
                await _dbContext.StripePurchases
                    .AnyAsync(x =>
                        x.StripeSessionId ==
                        stripeEvent.Id);

            if (alreadyProcessed)
            {
                return new OkResult();
            }

            using JsonDocument document =
                JsonDocument.Parse(
                    payload);

            JsonElement dataObject =
                document.RootElement
                    .GetProperty("data")
                    .GetProperty("object");

            string userId =
                string.Empty;

            string planKey =
                string.Empty;

            string recordKey =
                stripeEvent.Id;

            string recordPlanKey =
                string.Empty;

            bool shouldRecord =
                false;

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                case "checkout.session.async_payment_succeeded":
                {
                    string paymentStatus =
                        GetString(
                            dataObject,
                            "payment_status");

                    if (!string.Equals(
                            paymentStatus,
                            "paid",
                            StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(
                            paymentStatus,
                            "no_payment_required",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    string sessionId =
                        GetString(
                            dataObject,
                            "id");

                    userId =
                        GetMetadataValue(
                            dataObject,
                            "UserId");

                    planKey =
                        PonyUpPlanCatalog.NormalizeKey(
                            GetMetadataValue(
                                dataObject,
                                "PlanKey"));

                    if (string.IsNullOrWhiteSpace(
                            sessionId) ||
                        string.IsNullOrWhiteSpace(
                            userId) ||
                        planKey ==
                            PonyUpPlanCatalog.FreeKey)
                    {
                        break;
                    }

                    bool sessionAlreadyProcessed =
                        await _dbContext.StripePurchases
                            .AnyAsync(x =>
                                x.StripeSessionId ==
                                sessionId);

                    if (sessionAlreadyProcessed)
                    {
                        return new OkResult();
                    }

                    ApplicationUser? user =
                        await _dbContext.Users
                            .FirstOrDefaultAsync(
                                x => x.Id == userId);

                    if (user == null)
                    {
                        break;
                    }

                    await _planEntitlementService
                        .ApplyCheckoutAsync(
                            user,
                            planKey,
                            GetString(
                                dataObject,
                                "customer"),
                            GetString(
                                dataObject,
                                "subscription"),
                            GetMetadataValue(
                                dataObject,
                                "BillingInterval"));

                    recordKey =
                        sessionId;

                    recordPlanKey =
                        planKey;

                    shouldRecord =
                        true;

                    break;
                }

                case "invoice.paid":
                {
                    string billingReason =
                        GetString(
                            dataObject,
                            "billing_reason");

                    if (!string.Equals(
                            billingReason,
                            "subscription_cycle",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    userId =
                        GetSubscriptionUserIdFromInvoice(
                            dataObject);

                    if (string.IsNullOrWhiteSpace(
                            userId))
                    {
                        break;
                    }

                    ApplicationUser? user =
                        await _dbContext.Users
                            .FirstOrDefaultAsync(
                                x => x.Id == userId);

                    if (user == null)
                    {
                        break;
                    }

                    /*
                     * CurrentPlan is kept in sync by
                     * customer.subscription.updated.
                     * Use it here instead of stale
                     * subscription metadata so a portal
                     * upgrade/downgrade renews the plan
                     * the customer actually has now.
                     */
                    planKey =
                        PonyUpPlanCatalog.NormalizeKey(
                            user.CurrentPlan);

                    _planEntitlementService
                        .ApplyRenewal(
                            user,
                            planKey);

                    recordPlanKey =
                        $"webhook:{stripeEvent.Type}:{planKey}";

                    shouldRecord =
                        true;

                    break;
                }

                case "customer.subscription.updated":
                {
                    userId =
                        GetMetadataValue(
                            dataObject,
                            "UserId");

                    string subscriptionId =
                        GetString(
                            dataObject,
                            "id");

                    string customerId =
                        GetString(
                            dataObject,
                            "customer");

                    string priceId =
                        GetSubscriptionPriceId(
                            dataObject);

                    planKey =
                        _stripeCheckoutService
                            .ResolvePlanKeyFromPriceId(
                                priceId);

                    string billingInterval =
                        _stripeCheckoutService
                            .ResolveBillingIntervalFromPriceId(
                                priceId);

                    if (string.IsNullOrWhiteSpace(
                            userId) ||
                        planKey ==
                            PonyUpPlanCatalog.FreeKey)
                    {
                        break;
                    }

                    ApplicationUser? user =
                        await _dbContext.Users
                            .FirstOrDefaultAsync(
                                x => x.Id == userId);

                    if (user == null)
                    {
                        break;
                    }

                    string currentPlanKey =
                        PonyUpPlanCatalog.NormalizeKey(
                            user.CurrentPlan);

                    bool planChanged =
                        !string.Equals(
                            currentPlanKey,
                            planKey,
                            StringComparison.Ordinal);

                    bool cadenceChanged =
                        !string.IsNullOrWhiteSpace(
                            billingInterval) &&
                        !string.Equals(
                            user.SubscriptionBillingInterval,
                            billingInterval,
                            StringComparison.OrdinalIgnoreCase);

                    if (!string.IsNullOrWhiteSpace(
                            billingInterval))
                    {
                        user.SubscriptionBillingInterval =
                            billingInterval;
                    }

                    if (planChanged ||
                        cadenceChanged)
                    {
                        _planEntitlementService
                            .ApplyRenewal(
                                user,
                                planKey);
                    }

                    if (!string.IsNullOrWhiteSpace(
                            subscriptionId))
                    {
                        user.ActiveStripeSubscriptionId =
                            subscriptionId;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            customerId))
                    {
                        user.StripeCustomerId =
                            customerId;
                    }

                    recordPlanKey =
                        $"webhook:{stripeEvent.Type}:{planKey}";

                    shouldRecord =
                        true;

                    break;
                }

                case "customer.subscription.deleted":
                {
                    userId =
                        GetMetadataValue(
                            dataObject,
                            "UserId");

                    planKey =
                        PonyUpPlanCatalog.NormalizeKey(
                            GetMetadataValue(
                                dataObject,
                                "PlanKey"));

                    string subscriptionId =
                        GetString(
                            dataObject,
                            "id");

                    if (string.IsNullOrWhiteSpace(
                            userId))
                    {
                        break;
                    }

                    ApplicationUser? user =
                        await _dbContext.Users
                            .FirstOrDefaultAsync(
                                x => x.Id == userId);

                    if (user == null)
                    {
                        break;
                    }

                    shouldRecord =
                        await _planEntitlementService
                            .ApplyCancellationAsync(
                                user,
                                subscriptionId);

                    if (shouldRecord)
                    {
                        recordPlanKey =
                            $"webhook:{stripeEvent.Type}:{planKey}";
                    }

                    break;
                }
            }

            if (shouldRecord)
            {
                _dbContext.StripePurchases.Add(
                    new StripePurchase
                    {
                        UserId =
                            userId,

                        StripeSessionId =
                            recordKey,

                        PlanKey =
                            string.IsNullOrWhiteSpace(
                                recordPlanKey)
                                ? planKey
                                : recordPlanKey,

                        CreatedOn =
                            DateTime.UtcNow
                    });

                await _dbContext.SaveChangesAsync();
            }

            return new OkResult();
        }

        private static string
            GetSubscriptionUserIdFromInvoice(
                JsonElement invoice)
        {
            /*
             * Current Stripe invoices expose
             * subscription_details.metadata directly.
             * Keep the parent.subscription_details
             * fallback for compatibility with event
             * shapes used by earlier API versions.
             */
            if (invoice.TryGetProperty(
                    "subscription_details",
                    out JsonElement directDetails) &&
                directDetails.ValueKind ==
                    JsonValueKind.Object)
            {
                string directUserId =
                    GetMetadataValue(
                        directDetails,
                        "UserId");

                if (!string.IsNullOrWhiteSpace(
                        directUserId))
                {
                    return directUserId;
                }
            }

            if (invoice.TryGetProperty(
                    "parent",
                    out JsonElement parent) &&
                parent.ValueKind ==
                    JsonValueKind.Object &&
                parent.TryGetProperty(
                    "subscription_details",
                    out JsonElement parentDetails) &&
                parentDetails.ValueKind ==
                    JsonValueKind.Object)
            {
                return GetMetadataValue(
                    parentDetails,
                    "UserId");
            }

            return string.Empty;
        }

        private static string GetSubscriptionPriceId(
            JsonElement subscription)
        {
            if (!subscription.TryGetProperty(
                    "items",
                    out JsonElement items) ||
                items.ValueKind !=
                    JsonValueKind.Object ||
                !items.TryGetProperty(
                    "data",
                    out JsonElement data) ||
                data.ValueKind !=
                    JsonValueKind.Array)
            {
                return string.Empty;
            }

            JsonElement firstItem =
                data.EnumerateArray()
                    .FirstOrDefault();

            if (firstItem.ValueKind !=
                    JsonValueKind.Object ||
                !firstItem.TryGetProperty(
                    "price",
                    out JsonElement price) ||
                price.ValueKind !=
                    JsonValueKind.Object)
            {
                return string.Empty;
            }

            return GetString(
                price,
                "id");
        }

        private static string GetMetadataValue(
            JsonElement element,
            string key)
        {
            if (!element.TryGetProperty(
                    "metadata",
                    out JsonElement metadata) ||
                metadata.ValueKind !=
                    JsonValueKind.Object ||
                !metadata.TryGetProperty(
                    key,
                    out JsonElement value) ||
                value.ValueKind !=
                    JsonValueKind.String)
            {
                return string.Empty;
            }

            return value.GetString()
                ?? string.Empty;
        }

        private static string GetString(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement value) ||
                value.ValueKind !=
                    JsonValueKind.String)
            {
                return string.Empty;
            }

            return value.GetString()
                ?? string.Empty;
        }
    }
}
