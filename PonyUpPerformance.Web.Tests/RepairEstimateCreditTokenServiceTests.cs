using Microsoft.AspNetCore.DataProtection;
using PonyUpPerformance.Web.Services;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class RepairEstimateCreditTokenServiceTests
{
    [Fact]
    public void IssuedToken_IsValidOnlyForIssuingUser()
    {
        var provider =
            new EphemeralDataProtectionProvider();

        var service =
            new RepairEstimateCreditTokenService(
                provider);

        string token =
            service.Issue(
                "user-a");

        Assert.True(
            service.IsValid(
                token,
                "user-a"));

        Assert.False(
            service.IsValid(
                token,
                "user-b"));
    }

    [Fact]
    public void TamperedToken_IsRejected()
    {
        var provider =
            new EphemeralDataProtectionProvider();

        var service =
            new RepairEstimateCreditTokenService(
                provider);

        string token =
            service.Issue(
                "user-a");

        string tampered =
            token[..^1] +
            (token[^1] == 'A'
                ? "B"
                : "A");

        Assert.False(
            service.IsValid(
                tampered,
                "user-a"));
    }
}
