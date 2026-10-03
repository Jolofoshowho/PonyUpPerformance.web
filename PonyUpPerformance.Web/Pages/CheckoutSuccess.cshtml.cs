using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages
{
    public class CheckoutSuccessModel : PageModel
    {
        private readonly StripeCheckoutService _stripeCheckoutService;
        private readonly StripeFulfillmentService _stripeFulfillmentService;

        public string Message { get; set; } = "";

        public CheckoutSuccessModel(
            StripeCheckoutService stripeCheckoutService,
            StripeFulfillmentService stripeFulfillmentService)
        {
            _stripeCheckoutService = stripeCheckoutService;
            _stripeFulfillmentService = stripeFulfillmentService;
        }

        public async Task OnGetAsync(
            string session_id)
        {
            if (string.IsNullOrWhiteSpace(
                    session_id))
            {
                Message =
                    "Missing checkout session.";

                return;
            }

            Stripe.Checkout.Session session =
                await _stripeCheckoutService
                    .GetSessionAsync(
                        session_id);

            if (!string.Equals(
                    session.PaymentStatus,
                    "paid",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    session.PaymentStatus,
                    "no_payment_required",
                    StringComparison.OrdinalIgnoreCase))
            {
                Message =
                    "Payment has not completed.";

                return;
            }

            if (!session.Metadata.TryGetValue(
                    "UserId",
                    out string? userId) ||
                string.IsNullOrWhiteSpace(
                    userId) ||
                !session.Metadata.TryGetValue(
                    "PlanKey",
                    out string? rawPlanKey) ||
                string.IsNullOrWhiteSpace(
                    rawPlanKey))
            {
                Message =
                    "Checkout metadata is incomplete.";

                return;
            }

            string planKey =
                PonyUpPlanCatalog.NormalizeKey(
                    rawPlanKey);

            session.Metadata.TryGetValue(
                "BillingInterval",
                out string? billingInterval);

            CheckoutFulfillmentResult result =
                await _stripeFulfillmentService
                    .FulfillCheckoutAsync(
                        session_id,
                        userId,
                        planKey,
                        session.CustomerId,
                        session.SubscriptionId,
                        billingInterval);

            if (result ==
                CheckoutFulfillmentResult.UserNotFound)
            {
                Message =
                    "Payment completed, but the PonyUp account could not be matched.";

                return;
            }

            if (result ==
                CheckoutFulfillmentResult.InvalidRequest)
            {
                Message =
                    "Payment completed, but the checkout details were incomplete.";

                return;
            }

            PonyUpPlanAccess access =
                PonyUpPlanCatalog.Resolve(
                    planKey);

            Message =
                access.UnlimitedStandardAnalyses
                    ? $"Payment complete. {access.DisplayName} is active."
                    : "Payment complete. Your PonyUp access is active.";
        }
    }
}
