using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class StripeFulfillmentServiceTests
{
    [Fact]
    public async Task DuplicateQuickPackSession_IsFulfilledOnlyOnce()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "quickpack-idempotency-user",
                CurrentPlan = "Free",
                RemainingCredits = 1
            };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var planService =
            new PlanEntitlementService(db);

        var fulfillmentService =
            new StripeFulfillmentService(
                db,
                planService);

        CheckoutFulfillmentResult first =
            await fulfillmentService
                .FulfillCheckoutAsync(
                    "cs_duplicate_test",
                    user.Id,
                    PonyUpPlanCatalog.QuickPackKey,
                    "cus_test",
                    null,
                    "monthly");

        CheckoutFulfillmentResult second =
            await fulfillmentService
                .FulfillCheckoutAsync(
                    "cs_duplicate_test",
                    user.Id,
                    PonyUpPlanCatalog.QuickPackKey,
                    "cus_test",
                    null,
                    "monthly");

        Assert.Equal(
            CheckoutFulfillmentResult.Applied,
            first);

        Assert.Equal(
            CheckoutFulfillmentResult.AlreadyProcessed,
            second);

        Assert.Equal(
            6,
            user.RemainingCredits);

        Assert.Equal(
            1,
            await db.StripePurchases.CountAsync());
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
