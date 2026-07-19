using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IRecallService
    {
        Task<RecallResult> AnalyzeAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default);
    }
}
