using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PonyUpPerformance.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class SiteSmokeTests :
    IClassFixture<PonyUpFactory>
{
    private readonly HttpClient _client;

    public SiteSmokeTests(
        PonyUpFactory factory)
    {
        _client =
            factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/DecisionCenter")]
    [InlineData("/Pricing")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/Identity/Account/ForgotPassword")]
    [InlineData("/Identity/Account/ResendEmailConfirmation")]
    [InlineData("/RepairAnalyzer")]
    [InlineData("/BuyAnalyzer")]
    [InlineData("/SellAnalyzer")]
    [InlineData("/TradeAnalyzer")]
    [InlineData("/UpgradeAnalyzer")]
    [InlineData("/Resources")]
    [InlineData("/Privacy")]
    [InlineData("/Terms")]
    [InlineData("/DataDeletion")]
    [InlineData("/OwnerLogin")]
    public async Task PublicPages_RenderWithoutServerError(
        string path)
    {
        using HttpResponseMessage response =
            await _client.GetAsync(path);

        Assert.True(
            (int)response.StatusCode < 500,
            $"{path} returned {(int)response.StatusCode} {response.StatusCode}.");
    }

    [Fact]
    public async Task Login_UsesSupportLinkWhenEmailDeliveryIsNotConfigured()
    {
        string html =
            await _client.GetStringAsync(
                "/Identity/Account/Login");

        Assert.Contains(
            "Need account help?",
            html,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Forgot your password?",
            html,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdentityEmailSender_IsRegistered(
        PonyUpFactory factory)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        var sender =
            scope.ServiceProvider
                .GetService<IEmailSender<ApplicationUser>>();

        Assert.NotNull(sender);
        Assert.IsType<PonyUpPerformance.Web.Services.PonyUpIdentityEmailSender>(
            sender);
    }

    [Fact]
    public async Task Pricing_RendersNewTierNames()
    {
        string html =
            await _client.GetStringAsync(
                "/Pricing");

        Assert.Contains(
            "FULL THROTTLE",
            html,
            StringComparison.Ordinal);

        Assert.Contains(
            "REDLINE",
            html,
            StringComparison.Ordinal);

        Assert.Contains(
            "QUICK PACK",
            html,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Home_RoutesDecisionCardsToAnalyzerPages()
    {
        string html =
            await _client.GetStringAsync(
                "/");

        foreach (string route in new[]
        {
            "/RepairAnalyzer",
            "/BuyAnalyzer",
            "/SellAnalyzer",
            "/TradeAnalyzer",
            "/UpgradeAnalyzer"
        })
        {
            Assert.Contains(
                $"href=\"{route}\"",
                html,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DecisionCenter_RendersAllFiveActions()
    {
        string html =
            await _client.GetStringAsync(
                "/DecisionCenter");

        foreach (string label in new[]
        {
            "REPAIR",
            "BUY",
            "SELL",
            "TRADE",
            "UPGRADE"
        })
        {
            Assert.Contains(
                label,
                html,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UnknownRoute_DoesNotReturnServerError()
    {
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/this-route-should-not-exist");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}

public sealed class PonyUpFactory :
    WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Validation");
    }
}
