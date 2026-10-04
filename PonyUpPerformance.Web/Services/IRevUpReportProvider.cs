using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public sealed record RevUpProviderRequest
{
    public string Vin { get; init; } = string.Empty;
}

public sealed record RevUpProviderResult
{
    public bool Success { get; init; }

    public string ProviderName { get; init; } = string.Empty;

    public string ProviderReportId { get; init; } = string.Empty;

    public int? ExternalCostCents { get; init; }

    public string ReportJson { get; init; } = "{}";

    public string ErrorMessage { get; init; } = string.Empty;
}

public interface IRevUpReportProvider
{
    bool IsConfigured { get; }

    Task<RevUpProviderResult> PurchaseReportAsync(
        RevUpProviderRequest request,
        CancellationToken cancellationToken = default);
}

/*
 * Deliberately disabled until PonyUp has approved commercial provider terms
 * and credentials. This prevents an unfinished Redline feature from silently
 * purchasing external data.
 */
public sealed class UnavailableRevUpReportProvider :
    IRevUpReportProvider
{
    public bool IsConfigured =>
        false;

    public Task<RevUpProviderResult> PurchaseReportAsync(
        RevUpProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new RevUpProviderResult
            {
                Success = false,
                ErrorMessage =
                    "Premium vehicle-report provider is not configured."
            });
    }
}
