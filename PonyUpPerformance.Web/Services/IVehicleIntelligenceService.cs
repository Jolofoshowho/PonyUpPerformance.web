using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IVehicleIntelligenceService
    {
        Task<VehicleProfile> DecodeVinAsync(string vin);

        Task<VehicleProfile> EnrichVehicleAsync(VehicleProfile vehicle);

        Task<VehicleProfile?> GetCachedVehicleAsync(string vin);

        Task SaveVehicleAsync(VehicleProfile vehicle);

        Task<List<VehicleProfile>> GetRecentVehiclesAsync(string? userId = null);

        Task ClearRecentVehiclesAsync(string? userId = null);
    }
}
