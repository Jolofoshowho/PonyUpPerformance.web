using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public interface IVinDecoderService
    {
        Task<VehicleProfile> DecodeAsync(
            string vin,
            CancellationToken cancellationToken = default);
    }
}
