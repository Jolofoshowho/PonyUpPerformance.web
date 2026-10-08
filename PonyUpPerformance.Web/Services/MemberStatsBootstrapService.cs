using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public sealed class MemberStatsBootstrapService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemberStatsBootstrapService> _logger;

    public MemberStatsBootstrapService(
        IServiceScopeFactory scopeFactory,
        ILogger<MemberStatsBootstrapService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope =
            _scopeFactory.CreateAsyncScope();

        ApplicationDbContext db =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        int totalAccounts =
            await db.Users.CountAsync(
                cancellationToken);

        int ownerAccounts =
            await db.Users.CountAsync(
                x => x.Email == "lopezkb258@gmail.com",
                cancellationToken);

        int freeAccounts =
            await db.Users.CountAsync(
                x =>
                    string.IsNullOrEmpty(x.CurrentPlan) ||
                    x.CurrentPlan == "Free" ||
                    x.CurrentPlan == "free",
                cancellationToken);

        int activeSubscriptions =
            await db.Users.CountAsync(
                x => x.ActiveStripeSubscriptionId != "",
                cancellationToken);

        int quickPackBuyers =
            await db.StripePurchases
                .Where(
                    x => x.PlanKey == PonyUpPlanCatalog.QuickPackKey)
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(
                    cancellationToken);

        int betaRedemptions = 0;

        try
        {
            betaRedemptions =
                await db.BetaAccessRedemptions
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync(
                        cancellationToken);
        }
        catch
        {
            // The beta tables can still be initializing on first startup.
        }

        _logger.LogInformation(
            "PONYUP_MEMBER_STATS TotalAccounts={TotalAccounts} NonOwnerAccounts={NonOwnerAccounts} FreeAccounts={FreeAccounts} ActiveSubscriptions={ActiveSubscriptions} QuickPackBuyers={QuickPackBuyers} BetaTesters={BetaTesters}",
            totalAccounts,
            Math.Max(0, totalAccounts - ownerAccounts),
            freeAccounts,
            activeSubscriptions,
            quickPackBuyers,
            betaRedemptions);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
