using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services;

public sealed class RevUpReportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UsageCreditService _usageCreditService;
    private readonly IRevUpReportProvider _provider;

    public RevUpReportService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        UsageCreditService usageCreditService,
        IRevUpReportProvider provider)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _usageCreditService = usageCreditService;
        _provider = provider;
    }

    public async Task<RevUpReport?> GetExistingAsync(
        ClaimsPrincipal principal,
        string vin)
    {
        ApplicationUser? user =
            await _userManager.GetUserAsync(
                principal);

        string normalizedVin =
            NormalizeVin(vin);

        if (user == null ||
            normalizedVin.Length != 17)
        {
            return null;
        }

        return await _dbContext.RevUpReports
            .Where(x =>
                x.UserId == user.Id &&
                x.Vin == normalizedVin &&
                x.Status == "Complete")
            .OrderByDescending(x =>
                x.CompletedOn)
            .FirstOrDefaultAsync();
    }

    public async Task<RevUpReportRequestResult> RequestAsync(
        ClaimsPrincipal principal,
        string vin,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user =
            await _userManager.GetUserAsync(
                principal);

        if (user == null)
        {
            return RevUpReportRequestResult.Fail(
                "Sign in to request a Redline RevUp Report.");
        }

        string normalizedVin =
            NormalizeVin(vin);

        if (normalizedVin.Length != 17)
        {
            return RevUpReportRequestResult.Fail(
                "A valid 17-character VIN is required for a Redline RevUp Report.");
        }

        RevUpReport? cached =
            await GetExistingAsync(
                principal,
                normalizedVin);

        if (cached != null)
        {
            return RevUpReportRequestResult.FromExisting(
                cached);
        }

        UsageCreditStatus access =
            await _usageCreditService.GetStatusAsync(
                principal);

        bool hasReportAccess =
            access.UnlimitedRevUpReports ||
            access.RevUpReportsRemaining > 0 ||
            access.PlanKey ==
                PonyUpPlanCatalog.OwnerKey;

        if (!hasReportAccess)
        {
            return RevUpReportRequestResult.Fail(
                "This account does not have an available Redline RevUp Report.");
        }

        if (!_provider.IsConfigured)
        {
            return RevUpReportRequestResult.Fail(
                "Redline RevUp Reports are not available yet.");
        }

        var report =
            new RevUpReport
            {
                UserId =
                    user.Id,
                Vin =
                    normalizedVin,
                Status =
                    "Pending",
                CreatedOn =
                    DateTime.UtcNow
            };

        _dbContext.RevUpReports.Add(
            report);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        RevUpProviderResult providerResult;

        try
        {
            providerResult =
                await _provider.PurchaseReportAsync(
                    new RevUpProviderRequest
                    {
                        Vin = normalizedVin
                    },
                    cancellationToken);
        }
        catch (Exception ex)
        {
            report.Status =
                "Failed";

            report.ErrorMessage =
                ex.Message;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return RevUpReportRequestResult.Fail(
                "The premium vehicle report could not be completed.");
        }

        report.ProviderName =
            providerResult.ProviderName;

        report.ProviderReportId =
            providerResult.ProviderReportId;

        report.ExternalCostCents =
            providerResult.ExternalCostCents;

        if (!providerResult.Success)
        {
            report.Status =
                "Failed";

            report.ErrorMessage =
                providerResult.ErrorMessage;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return RevUpReportRequestResult.Fail(
                string.IsNullOrWhiteSpace(
                    providerResult.ErrorMessage)
                    ? "The premium vehicle report could not be completed."
                    : providerResult.ErrorMessage);
        }

        report.Status =
            "Complete";

        report.ReportJson =
            string.IsNullOrWhiteSpace(
                providerResult.ReportJson)
                ? "{}"
                : providerResult.ReportJson;

        report.CompletedOn =
            DateTime.UtcNow;

        if (!access.UnlimitedRevUpReports &&
            access.PlanKey !=
                PonyUpPlanCatalog.OwnerKey)
        {
            user.RevUpReportsRemaining =
                Math.Max(
                    0,
                    user.RevUpReportsRemaining - 1);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return RevUpReportRequestResult.FromNew(
            report);
    }

    private static string NormalizeVin(
        string? vin)
    {
        return (vin ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }
}

public sealed record RevUpReportRequestResult
{
    public bool Success { get; init; }

    public bool ReusedExistingReport { get; init; }

    public string Message { get; init; } = string.Empty;

    public RevUpReport? Report { get; init; }

    public static RevUpReportRequestResult Fail(
        string message) =>
        new()
        {
            Success = false,
            Message = message
        };

    public static RevUpReportRequestResult FromExisting(
        RevUpReport report) =>
        new()
        {
            Success = true,
            ReusedExistingReport = true,
            Message =
                "Previously purchased report loaded.",
            Report = report
        };

    public static RevUpReportRequestResult FromNew(
        RevUpReport report) =>
        new()
        {
            Success = true,
            Message =
                "Redline RevUp Report complete.",
            Report = report
        };
}
