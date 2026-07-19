using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IReliabilityService
    {
        Task<ReliabilityResult> AnalyzeAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default);
    }
}
