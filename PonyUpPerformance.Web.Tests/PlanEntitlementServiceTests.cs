using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class PlanEntitlementServiceTests
{
    [Fact]
    public async Task ProMonthly_DoesNotRefreshCreditsByClock()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "pro-monthly-user",
                CurrentPlan = "Free"
            };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service =
            new PlanEntitlementService(db);

        await service.ApplyCheckoutAsync(
            user,
            PonyUpPlanCatalog.ProKey,
            "cus_test",
            "sub_test",
            "monthly");

        Assert.Equal(10, user.SubscriptionCredits);
        Assert.Equal("monthly", user.SubscriptionBillingInterval);
        Assert.Null(user.NextSubscriptionCreditRefreshOn);

        user.SubscriptionCredits = 0;
        user.NextSubscriptionCreditRefreshOn =
            DateTime.UtcNow.AddMinutes(-10);

        await db.SaveChangesAsync();

        await service.RefreshMonthlyEntitlementsAsync(
            user);

        Assert.Equal(0, user.SubscriptionCredits);
    }

    [Fact]
    public async Task ProAnnual_RefreshesTenCreditsMonthly()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "pro-annual-user",
                CurrentPlan = "Free"
            };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service =
            new PlanEntitlementService(db);

        await service.ApplyCheckoutAsync(
            user,
            PonyUpPlanCatalog.ProKey,
            "cus_test",
            "sub_test",
            "annual");

        Assert.Equal(10, user.SubscriptionCredits);
        Assert.Equal("annual", user.SubscriptionBillingInterval);
        Assert.NotNull(user.NextSubscriptionCreditRefreshOn);

        user.SubscriptionCredits = 0;
        user.NextSubscriptionCreditRefreshOn =
            DateTime.UtcNow.AddMinutes(-10);

        await db.SaveChangesAsync();

        await service.RefreshMonthlyEntitlementsAsync(
            user);

        Assert.Equal(10, user.SubscriptionCredits);

        Assert.True(
            user.NextSubscriptionCreditRefreshOn >
            DateTime.UtcNow);
    }

    [Fact]
    public async Task Cancellation_ReturnsQuickPackBuyerToQuickPack()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "quickpack-owner",
                CurrentPlan = "Pro",
                ActiveStripeSubscriptionId = "sub_current",
                StripeCustomerId = "cus_test",
                SubscriptionBillingInterval = "monthly",
                SubscriptionCredits = 7
            };

        db.Users.Add(user);

        db.StripePurchases.Add(
            new StripePurchase
            {
                UserId = user.Id,
                StripeSessionId = "cs_quickpack",
                PlanKey = PonyUpPlanCatalog.QuickPackKey,
                CreatedOn = DateTime.UtcNow
            });

        await db.SaveChangesAsync();

        var service =
            new PlanEntitlementService(db);

        bool changed =
            await service.ApplyCancellationAsync(
                user,
                "sub_current");

        Assert.True(changed);
        Assert.Equal("Quick Pack", user.CurrentPlan);
        Assert.Equal(0, user.SubscriptionCredits);
        Assert.Equal(string.Empty, user.ActiveStripeSubscriptionId);
        Assert.Equal(string.Empty, user.SubscriptionBillingInterval);
    }

    [Fact]
    public async Task Cancellation_IgnoresOldSubscriptionDeletionAfterUpgrade()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "upgrade-user",
                CurrentPlan = "Full Throttle",
                ActiveStripeSubscriptionId = "sub_new"
            };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service =
            new PlanEntitlementService(db);

        bool changed =
            await service.ApplyCancellationAsync(
                user,
                "sub_old");

        Assert.False(changed);
        Assert.Equal("Full Throttle", user.CurrentPlan);
        Assert.Equal("sub_new", user.ActiveStripeSubscriptionId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(
            options);
    }
}
