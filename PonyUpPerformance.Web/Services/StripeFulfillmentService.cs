using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public enum CheckoutFulfillmentResult
{
    Applied,
    AlreadyProcessed,
    UserNotFound,
    InvalidRequest
}

public sealed class StripeFulfillmentService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        InProcessSessionLocks =
            new(StringComparer.Ordinal);

    private readonly ApplicationDbContext _dbContext;
    private readonly PlanEntitlementService _planEntitlementService;

    public StripeFulfillmentService(
        ApplicationDbContext dbContext,
        PlanEntitlementService planEntitlementService)
    {
        _dbContext = dbContext;
        _planEntitlementService = planEntitlementService;
    }

    public async Task<CheckoutFulfillmentResult> FulfillCheckoutAsync(
        string sessionId,
        string userId,
        string planKey,
        string? stripeCustomerId,
        string? stripeSubscriptionId,
        string? billingInterval)
    {
        string normalizedSessionId =
            (sessionId ?? string.Empty).Trim();

        string normalizedUserId =
            (userId ?? string.Empty).Trim();

        string normalizedPlanKey =
            PonyUpPlanCatalog.NormalizeKey(
                planKey);

        if (string.IsNullOrWhiteSpace(
                normalizedSessionId) ||
            string.IsNullOrWhiteSpace(
                normalizedUserId) ||
            normalizedPlanKey ==
                PonyUpPlanCatalog.FreeKey)
        {
            return CheckoutFulfillmentResult.InvalidRequest;
        }

        if (IsPostgreSql())
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync();

            await _dbContext.Database
                .ExecuteSqlInterpolatedAsync(
                    $"""
                    SELECT pg_advisory_xact_lock(
                        hashtext({normalizedSessionId})::bigint);
                    """);

            CheckoutFulfillmentResult result =
                await FulfillCoreAsync(
                    normalizedSessionId,
                    normalizedUserId,
                    normalizedPlanKey,
                    stripeCustomerId,
                    stripeSubscriptionId,
                    billingInterval);

            await transaction.CommitAsync();

            return result;
        }

        /*
         * Runtime validation uses EF's in-memory provider.
         * Keep those calls serialized too so tests exercise
         * the same one-session/one-fulfillment contract.
         */
        SemaphoreSlim gate =
            InProcessSessionLocks.GetOrAdd(
                normalizedSessionId,
                _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync();

        try
        {
            return await FulfillCoreAsync(
                normalizedSessionId,
                normalizedUserId,
                normalizedPlanKey,
                stripeCustomerId,
                stripeSubscriptionId,
                billingInterval);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<CheckoutFulfillmentResult>
        FulfillCoreAsync(
            string sessionId,
            string userId,
            string planKey,
            string? stripeCustomerId,
            string? stripeSubscriptionId,
            string? billingInterval)
    {
        bool alreadyProcessed =
            await _dbContext.StripePurchases
                .AnyAsync(x =>
                    x.StripeSessionId ==
                    sessionId);

        if (alreadyProcessed)
        {
            return CheckoutFulfillmentResult.AlreadyProcessed;
        }

        ApplicationUser? user =
            await _dbContext.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId);

        if (user == null)
        {
            return CheckoutFulfillmentResult.UserNotFound;
        }

        await _planEntitlementService
            .ApplyCheckoutAsync(
                user,
                planKey,
                stripeCustomerId,
                stripeSubscriptionId,
                billingInterval);

        _dbContext.StripePurchases.Add(
            new StripePurchase
            {
                UserId =
                    user.Id,

                StripeSessionId =
                    sessionId,

                PlanKey =
                    planKey,

                CreatedOn =
                    DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync();

        return CheckoutFulfillmentResult.Applied;
    }

    private bool IsPostgreSql()
    {
        string providerName =
            _dbContext.Database.ProviderName
            ?? string.Empty;

        return providerName.Contains(
            "Npgsql",
            StringComparison.OrdinalIgnoreCase);
    }
}
