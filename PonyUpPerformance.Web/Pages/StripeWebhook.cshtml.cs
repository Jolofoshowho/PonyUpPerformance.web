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

        public StripeWebhookModel(
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            PlanEntitlementService planEntitlementService)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _planEntitlementService = planEntitlementService;
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

            bool shouldRecord =
                false;

            switch (stripeEvent.Type)
            {
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

                    (userId, planKey) =
                        GetSubscriptionMetadataFromInvoice(
                            dataObject);

                    if (string.IsNullOrWhiteSpace(
                            userId) ||
                        string.IsNullOrWhiteSpace(
                            planKey))
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

                    planKey =
                        PonyUpPlanCatalog.NormalizeKey(
                            planKey);

                    _planEntitlementService
                        .ApplyRenewal(
                            user,
                            planKey);

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
                            stripeEvent.Id,

                        PlanKey =
                            $"webhook:{stripeEvent.Type}:{planKey}",

                        CreatedOn =
                            DateTime.UtcNow
                    });

                await _dbContext.SaveChangesAsync();
            }

            return new OkResult();
        }

        private static (
            string UserId,
            string PlanKey)
            GetSubscriptionMetadataFromInvoice(
                JsonElement invoice)
        {
            if (!invoice.TryGetProperty(
                    "parent",
                    out JsonElement parent) ||
                parent.ValueKind !=
                    JsonValueKind.Object ||
                !parent.TryGetProperty(
                    "subscription_details",
                    out JsonElement subscriptionDetails) ||
                subscriptionDetails.ValueKind !=
                    JsonValueKind.Object)
            {
                return (
                    string.Empty,
                    string.Empty);
            }

            return (
                GetMetadataValue(
                    subscriptionDetails,
                    "UserId"),

                GetMetadataValue(
                    subscriptionDetails,
                    "PlanKey"));
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
