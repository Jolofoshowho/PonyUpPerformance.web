using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IVehicleSpecEnrichmentService
    {
        Task<VehicleProfile> EnrichAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default);
    }
}
