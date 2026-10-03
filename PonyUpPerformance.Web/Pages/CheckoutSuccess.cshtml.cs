using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages
{
    public class CheckoutSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly StripeCheckoutService _stripeCheckoutService;
        private readonly PlanEntitlementService _planEntitlementService;

        public string Message { get; set; } = "";

        public CheckoutSuccessModel(
            ApplicationDbContext dbContext,
            StripeCheckoutService stripeCheckoutService,
            PlanEntitlementService planEntitlementService)
        {
            _dbContext = dbContext;
            _stripeCheckoutService = stripeCheckoutService;
            _planEntitlementService = planEntitlementService;
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

            bool alreadyProcessed =
                await _dbContext.StripePurchases
                    .AnyAsync(x =>
                        x.StripeSessionId ==
                        session_id);

            if (alreadyProcessed)
            {
                Message =
                    "Purchase already processed.";

                return;
            }

            Stripe.Checkout.Session session =
                await _stripeCheckoutService
                    .GetSessionAsync(
                        session_id);

            if (session.PaymentStatus !=
                "paid")
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

            ApplicationUser? user =
                await _dbContext.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == userId);

            if (user == null)
            {
                Message =
                    "User not found.";

                return;
            }

            string planKey =
                PonyUpPlanCatalog.NormalizeKey(
                    rawPlanKey);

            session.Metadata.TryGetValue(
                "BillingInterval",
                out string? billingInterval);

            await _planEntitlementService
                .ApplyCheckoutAsync(
                    user,
                    planKey,
                    session.CustomerId,
                    session.SubscriptionId,
                    billingInterval);

            _dbContext.StripePurchases.Add(
                new StripePurchase
                {
                    UserId =
                        user.Id,

                    StripeSessionId =
                        session_id,

                    PlanKey =
                        planKey,

                    CreatedOn =
                        DateTime.UtcNow
                });

            await _dbContext.SaveChangesAsync();

            PonyUpPlanAccess access =
                PonyUpPlanCatalog.Resolve(
                    planKey);

            Message =
                access.UnlimitedStandardAnalyses
                    ? $"Payment complete. {access.DisplayName} is active."
                    : $"Payment complete. Your PonyUp access is active.";
        }
    }
}
