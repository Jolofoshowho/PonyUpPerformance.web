using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class BetaAccessCodeServiceTests
{
    [Fact]
    public async Task BetaCode_AddsFiveCredits_OnlyOncePerAccount()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "beta-user",
                Email = "beta@example.com",
                UserName = "beta@example.com",
                RemainingCredits = 1,
                CurrentPlan = "Free"
            };

        const string rawCode =
            "PONYUP-BETA5";

        var code =
            new BetaAccessCode
            {
                CodeHash =
                    BetaAccessCodeService.HashCode(
                        BetaAccessCodeService.NormalizeCode(
                            rawCode)),

                CreditsGranted = 5,
                MaxRedemptions = 10,
                IsActive = true
            };

        db.Users.Add(user);
        db.BetaAccessCodes.Add(code);

        await db.SaveChangesAsync();

        var service =
            new BetaAccessCodeService(
                db);

        BetaAccessRedeemResult first =
            await service.RedeemAsync(
                user,
                rawCode);

        BetaAccessRedeemResult second =
            await service.RedeemAsync(
                user,
                rawCode);

        Assert.Equal(
            BetaAccessRedeemStatus.Applied,
            first.Status);

        Assert.Equal(
            5,
            first.CreditsGranted);

        Assert.Equal(
            6,
            user.RemainingCredits);

        Assert.Equal(
            BetaAccessRedeemStatus.AlreadyRedeemed,
            second.Status);

        Assert.Equal(
            0,
            second.CreditsGranted);

        Assert.Equal(
            6,
            user.RemainingCredits);

        Assert.Equal(
            1,
            await db.BetaAccessRedemptions.CountAsync());

        Assert.Equal(
            1,
            code.RedemptionCount);
    }

    [Fact]
    public async Task InvalidBetaCode_DoesNotChangeCredits()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var user =
            new ApplicationUser
            {
                Id = "beta-invalid-user",
                Email = "invalid@example.com",
                UserName = "invalid@example.com",
                RemainingCredits = 1,
                CurrentPlan = "Free"
            };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service =
            new BetaAccessCodeService(
                db);

        BetaAccessRedeemResult result =
            await service.RedeemAsync(
                user,
                "NOT-A-REAL-CODE");

        Assert.Equal(
            BetaAccessRedeemStatus.InvalidCode,
            result.Status);

        Assert.Equal(
            1,
            user.RemainingCredits);

        Assert.Empty(
            db.BetaAccessRedemptions);
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
