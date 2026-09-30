using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public sealed class PlanEntitlementService
{
    private readonly ApplicationDbContext _dbContext;

    public PlanEntitlementService(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ApplyCheckoutAsync(
        ApplicationUser user,
        string planKey,
        string? stripeCustomerId,
        string? stripeSubscriptionId)
    {
        string normalized =
            PonyUpPlanCatalog.NormalizeKey(
                planKey);

        switch (normalized)
        {
            case PonyUpPlanCatalog.QuickPackKey:
                user.RemainingCredits += 5;

                if (PlanRank(user.CurrentPlan) <
                    PlanRank(PonyUpPlanCatalog.QuickPackKey))
                {
                    user.CurrentPlan = "Quick Pack";
                }

                break;

            case PonyUpPlanCatalog.ProKey:
                user.CurrentPlan = "Pro";
                user.SubscriptionCredits = 10;
                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);
                break;

            case PonyUpPlanCatalog.FullThrottleKey:
                user.CurrentPlan = "Full Throttle";
                user.SubscriptionCredits = 0;
                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);
                break;

            case PonyUpPlanCatalog.RedlineKey:
                user.CurrentPlan = "Redline";
                user.SubscriptionCredits = 0;
                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);
                break;

            case PonyUpPlanCatalog.RedlinePlusKey:
                user.CurrentPlan = "Redline+";
                user.SubscriptionCredits = 0;
                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);
                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported PonyUp plan.");
        }

        await Task.CompletedTask;
    }

    public void ApplyRenewal(
        ApplicationUser user,
        string planKey)
    {
        string normalized =
            PonyUpPlanCatalog.NormalizeKey(
                planKey);

        switch (normalized)
        {
            case PonyUpPlanCatalog.ProKey:
                user.CurrentPlan = "Pro";

                // Pro includes 10 standard analyses per paid billing cycle.
                // Unused subscription credits do not accumulate indefinitely.
                user.SubscriptionCredits = 10;
                break;

            case PonyUpPlanCatalog.FullThrottleKey:
                user.CurrentPlan = "Full Throttle";
                user.SubscriptionCredits = 0;
                break;

            case PonyUpPlanCatalog.RedlineKey:
                user.CurrentPlan = "Redline";
                user.SubscriptionCredits = 0;
                break;

            case PonyUpPlanCatalog.RedlinePlusKey:
                user.CurrentPlan = "Redline+";
                user.SubscriptionCredits = 0;
                break;
        }
    }

    public async Task<bool> ApplyCancellationAsync(
        ApplicationUser user,
        string? deletedSubscriptionId)
    {
        if (!string.IsNullOrWhiteSpace(
                user.ActiveStripeSubscriptionId) &&
            !string.IsNullOrWhiteSpace(
                deletedSubscriptionId) &&
            !string.Equals(
                user.ActiveStripeSubscriptionId,
                deletedSubscriptionId,
                StringComparison.Ordinal))
        {
            // Ignore deletion of an older subscription after an upgrade.
            return false;
        }

        bool hasQuickPack =
            await _dbContext.StripePurchases
                .AnyAsync(x =>
                    x.UserId == user.Id &&
                    x.PlanKey ==
                        PonyUpPlanCatalog.QuickPackKey);

        user.SubscriptionCredits = 0;
        user.ActiveStripeSubscriptionId = string.Empty;

        user.CurrentPlan =
            hasQuickPack
                ? "Quick Pack"
                : "Free";

        return true;
    }

    private static void SetStripeSubscription(
        ApplicationUser user,
        string? stripeCustomerId,
        string? stripeSubscriptionId)
    {
        if (!string.IsNullOrWhiteSpace(
                stripeCustomerId))
        {
            user.StripeCustomerId =
                stripeCustomerId;
        }

        if (!string.IsNullOrWhiteSpace(
                stripeSubscriptionId))
        {
            user.ActiveStripeSubscriptionId =
                stripeSubscriptionId;
        }
    }

    private static int PlanRank(
        string? plan)
    {
        return PonyUpPlanCatalog.NormalizeKey(
            plan) switch
        {
            PonyUpPlanCatalog.QuickPackKey => 1,
            PonyUpPlanCatalog.ProKey => 2,
            PonyUpPlanCatalog.FullThrottleKey => 3,
            PonyUpPlanCatalog.RedlineKey => 4,
            PonyUpPlanCatalog.RedlinePlusKey => 5,
            PonyUpPlanCatalog.OwnerKey => 99,
            _ => 0
        };
    }
}
