using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IMarketValueService
    {
        Task<MarketValueResult> AnalyzeAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default);
    }
}
