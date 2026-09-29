using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using Stripe;

namespace PonyUpPerformance.Web.Pages
{
    [IgnoreAntiforgeryToken]
    public class StripeWebhookModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public StripeWebhookModel(
            IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string webhookSecret =
                _configuration["Stripe:WebhookSecret"]
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(webhookSecret))
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
                JsonDocument.Parse(payload);

            JsonElement dataObject =
                document.RootElement
                    .GetProperty("data")
                    .GetProperty("object");

            string userId = string.Empty;
            string planKey = string.Empty;
            bool shouldRecord = false;

            switch (stripeEvent.Type)
            {
                case "invoice.paid":
                {
                    string billingReason =
                        GetString(
                            dataObject,
                            "billing_reason");

                    if (string.Equals(
                            billingReason,
                            "subscription_cycle",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        (userId, planKey) =
                            GetSubscriptionMetadataFromInvoice(
                                dataObject);

                        if (!string.IsNullOrWhiteSpace(userId) &&
                            !string.IsNullOrWhiteSpace(planKey))
                        {
                            await ApplyRecurringPlanAsync(
                                userId,
                                planKey);

                            shouldRecord = true;
                        }
                    }

                    break;
                }

                case "customer.subscription.deleted":
                {
                    userId =
                        GetMetadataValue(
                            dataObject,
                            "UserId");

                    planKey =
                        GetMetadataValue(
                            dataObject,
                            "PlanKey");

                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        ApplicationUser? user =
                            await _dbContext.Users
                                .FirstOrDefaultAsync(
                                    x => x.Id == userId);

                        if (user != null)
                        {
                            user.CurrentPlan = "Free";
                            shouldRecord = true;
                        }
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

        private async Task ApplyRecurringPlanAsync(
            string userId,
            string planKey)
        {
            ApplicationUser? user =
                await _dbContext.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == userId);

            if (user == null)
            {
                return;
            }

            switch (
                planKey.Trim().ToLowerInvariant())
            {
                case "pro":
                    user.CurrentPlan = "Pro";
                    user.RemainingCredits += 10;
                    break;

                case "unlimited":
                    user.CurrentPlan = "Unlimited";
                    break;
            }
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
