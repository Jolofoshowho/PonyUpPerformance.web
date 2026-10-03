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

    public Task ApplyCheckoutAsync(
        ApplicationUser user,
        string planKey,
        string? stripeCustomerId,
        string? stripeSubscriptionId,
        string? billingInterval = null)
    {
        string normalized =
            PonyUpPlanCatalog.NormalizeKey(
                planKey);

        string normalizedBilling =
            NormalizeBillingInterval(
                billingInterval);

        DateTime now =
            DateTime.UtcNow;

        switch (normalized)
        {
            case PonyUpPlanCatalog.QuickPackKey:
                user.RemainingCredits += 5;

                if (PlanRank(user.CurrentPlan) <
                    PlanRank(PonyUpPlanCatalog.QuickPackKey))
                {
                    user.CurrentPlan =
                        "Quick Pack";
                }

                break;

            case PonyUpPlanCatalog.ProKey:
                user.CurrentPlan =
                    "Pro";

                user.SubscriptionCredits =
                    10;

                user.SubscriptionBillingInterval =
                    normalizedBilling;

                user.NextSubscriptionCreditRefreshOn =
                    normalizedBilling == "annual"
                        ? now.AddMonths(1)
                        : null;

                ClearRevUpAllowance(
                    user);

                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);

                break;

            case PonyUpPlanCatalog.FullThrottleKey:
                user.CurrentPlan =
                    "Full Throttle";

                user.SubscriptionCredits =
                    0;

                user.SubscriptionBillingInterval =
                    normalizedBilling;

                user.NextSubscriptionCreditRefreshOn =
                    null;

                ClearRevUpAllowance(
                    user);

                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);

                break;

            case PonyUpPlanCatalog.RedlineKey:
                user.CurrentPlan =
                    "Redline";

                user.SubscriptionCredits =
                    0;

                user.NextSubscriptionCreditRefreshOn =
                    null;

                user.SubscriptionBillingInterval =
                    normalizedBilling;

                user.RevUpReportsRemaining =
                    5;

                user.NextRevUpReportRefreshOn =
                    normalizedBilling == "annual"
                        ? now.AddMonths(1)
                        : null;

                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);

                break;

            case PonyUpPlanCatalog.RedlinePlusKey:
                user.CurrentPlan =
                    "Redline+";

                user.SubscriptionCredits =
                    0;

                user.NextSubscriptionCreditRefreshOn =
                    null;

                user.SubscriptionBillingInterval =
                    normalizedBilling;

                user.RevUpReportsRemaining =
                    0;

                user.NextRevUpReportRefreshOn =
                    null;

                SetStripeSubscription(
                    user,
                    stripeCustomerId,
                    stripeSubscriptionId);

                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported PonyUp plan.");
        }

        return Task.CompletedTask;
    }

    public void ApplyRenewal(
        ApplicationUser user,
        string planKey)
    {
        string normalized =
            PonyUpPlanCatalog.NormalizeKey(
                planKey);

        DateTime now =
            DateTime.UtcNow;

        switch (normalized)
        {
            case PonyUpPlanCatalog.ProKey:
                user.CurrentPlan =
                    "Pro";

                user.SubscriptionCredits =
                    10;

                user.NextSubscriptionCreditRefreshOn =
                    IsAnnual(user)
                        ? now.AddMonths(1)
                        : null;

                break;

            case PonyUpPlanCatalog.FullThrottleKey:
                user.CurrentPlan =
                    "Full Throttle";

                user.SubscriptionCredits =
                    0;

                user.NextSubscriptionCreditRefreshOn =
                    null;

                break;

            case PonyUpPlanCatalog.RedlineKey:
                user.CurrentPlan =
                    "Redline";

                user.RevUpReportsRemaining =
                    5;

                user.NextRevUpReportRefreshOn =
                    IsAnnual(user)
                        ? now.AddMonths(1)
                        : null;

                break;

            case PonyUpPlanCatalog.RedlinePlusKey:
                user.CurrentPlan =
                    "Redline+";

                user.RevUpReportsRemaining =
                    0;

                user.NextRevUpReportRefreshOn =
                    null;

                break;
        }
    }

    public async Task RefreshMonthlyEntitlementsAsync(
        ApplicationUser user)
    {
        string planKey =
            PonyUpPlanCatalog.NormalizeKey(
                user.CurrentPlan);

        DateTime now =
            DateTime.UtcNow;

        bool changed =
            false;

        if (planKey ==
                PonyUpPlanCatalog.ProKey &&
            IsAnnual(user))
        {
            if (!user.NextSubscriptionCreditRefreshOn.HasValue)
            {
                user.NextSubscriptionCreditRefreshOn =
                    now.AddMonths(1);

                changed =
                    true;
            }
            else if (user.NextSubscriptionCreditRefreshOn.Value <=
                     now)
            {
                user.SubscriptionCredits =
                    10;

                user.NextSubscriptionCreditRefreshOn =
                    AdvanceMonthly(
                        user.NextSubscriptionCreditRefreshOn.Value,
                        now);

                changed =
                    true;
            }
        }

        if (planKey ==
                PonyUpPlanCatalog.RedlineKey &&
            IsAnnual(user))
        {
            if (!user.NextRevUpReportRefreshOn.HasValue)
            {
                user.NextRevUpReportRefreshOn =
                    now.AddMonths(1);

                if (user.RevUpReportsRemaining <= 0)
                {
                    user.RevUpReportsRemaining =
                        5;
                }

                changed =
                    true;
            }
            else if (user.NextRevUpReportRefreshOn.Value <=
                     now)
            {
                user.RevUpReportsRemaining =
                    5;

                user.NextRevUpReportRefreshOn =
                    AdvanceMonthly(
                        user.NextRevUpReportRefreshOn.Value,
                        now);

                changed =
                    true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync();
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

        user.SubscriptionCredits =
            0;

        user.NextSubscriptionCreditRefreshOn =
            null;

        ClearRevUpAllowance(
            user);

        user.ActiveStripeSubscriptionId =
            string.Empty;

        user.SubscriptionBillingInterval =
            string.Empty;

        user.CurrentPlan =
            hasQuickPack
                ? "Quick Pack"
                : "Free";

        return true;
    }

    private static DateTime AdvanceMonthly(
        DateTime scheduled,
        DateTime now)
    {
        DateTime next =
            scheduled;

        do
        {
            next =
                next.AddMonths(1);
        }
        while (next <= now);

        return next;
    }

    private static void ClearRevUpAllowance(
        ApplicationUser user)
    {
        user.RevUpReportsRemaining =
            0;

        user.NextRevUpReportRefreshOn =
            null;
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

    private static bool IsAnnual(
        ApplicationUser user)
    {
        return string.Equals(
            user.SubscriptionBillingInterval,
            "annual",
            StringComparison.OrdinalIgnoreCase);
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
