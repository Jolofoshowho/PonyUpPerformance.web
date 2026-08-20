using System.Net.Http.Json;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public sealed class NhtsaVehicleService : IVinDecoderService
    {
        private readonly HttpClient _httpClient;

        public NhtsaVehicleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VehicleProfile> DecodeAsync(
            string vin,
            CancellationToken cancellationToken = default)
        {
            vin = (vin ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            var profile = new VehicleProfile
            {
                Vin = vin,
                DecodeSource = "NHTSA vPIC",
                DecodedAtUtc = DateTime.UtcNow,
                DecodeSuccessful = false
            };

            if (string.IsNullOrWhiteSpace(vin))
            {
                profile.DecodeWarnings.Add(
                    "No VIN was entered. You can continue without VIN decoding.");

                return profile;
            }

            try
            {
                var url =
                    "https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVinValues/" +
                    $"{Uri.EscapeDataString(vin)}?format=json";

                var response =
                    await _httpClient.GetFromJsonAsync<VpicResponse>(
                        url,
                        cancellationToken);

                var vehicle = response?.Results?.FirstOrDefault();

                if (vehicle == null)
                {
                    profile.DecodeWarnings.Add(
                        "NHTSA returned no vehicle information.");

                    return profile;
                }

                profile.Year = ParseNullableInt(vehicle.ModelYear);

                profile.Make = Clean(vehicle.Make);
                profile.Model = Clean(vehicle.Model);
                profile.Trim = Clean(vehicle.Trim);
                profile.Series = Clean(vehicle.Series);

                profile.Engine = BuildEngineDescription(vehicle);
                profile.EngineDisplacement = Clean(vehicle.DisplacementL);
                profile.EngineCylinders = Clean(vehicle.EngineCylinders);

                profile.Transmission = Clean(vehicle.TransmissionStyle);
                profile.TransmissionStyle = Clean(vehicle.TransmissionStyle);

                profile.Drivetrain = Clean(vehicle.DriveType);
                profile.BodyStyle = Clean(vehicle.BodyClass);
                profile.VehicleType = Clean(vehicle.VehicleType);

                profile.FuelType = Clean(vehicle.FuelTypePrimary);

                profile.Manufacturer = Clean(vehicle.Manufacturer);

                profile.PlantCountry = Clean(vehicle.PlantCountry);
                profile.PlantState = Clean(vehicle.PlantState);
                profile.PlantCity = Clean(vehicle.PlantCity);

                profile.BaseMsrp = ParseNullableDecimal(vehicle.BasePrice);

                bool hasVehicleIdentity =
                    profile.Year.HasValue ||
                    !string.IsNullOrWhiteSpace(profile.Make) ||
                    !string.IsNullOrWhiteSpace(profile.Model);

                profile.DecodeSuccessful = hasVehicleIdentity;

                if (!string.IsNullOrWhiteSpace(vehicle.ErrorCode) &&
                    vehicle.ErrorCode != "0" &&
                    !string.IsNullOrWhiteSpace(vehicle.ErrorText))
                {
                    profile.DecodeWarnings.Add(
                        vehicle.ErrorText.Trim());
                }

                if (!hasVehicleIdentity)
                {
                    profile.DecodeWarnings.Add(
                        "The VIN could not be decoded into a recognizable vehicle.");
                }

                return profile;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                profile.DecodeWarnings.Add(
                    "PonyUp could not reach the NHTSA VIN decoder. " +
                    "You can continue without VIN decoding.");

                return profile;
            }
        }

        private static int? ParseNullableInt(string? value)
        {
            return int.TryParse(value, out var result)
                ? result
                : null;
        }

        private static decimal? ParseNullableDecimal(string? value)
        {
            return decimal.TryParse(value, out var result)
                ? result
                : null;
        }

        private static string Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static string BuildEngineDescription(
            VpicVehicle vehicle)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(vehicle.DisplacementL))
            {
                parts.Add($"{vehicle.DisplacementL.Trim()}L");
            }

            if (!string.IsNullOrWhiteSpace(vehicle.EngineCylinders))
            {
                parts.Add($"{vehicle.EngineCylinders.Trim()} Cyl");
            }

            if (!string.IsNullOrWhiteSpace(vehicle.EngineModel))
            {
                parts.Add(vehicle.EngineModel.Trim());
            }

            return string.Join(" ", parts);
        }

        private sealed class VpicResponse
        {
            public List<VpicVehicle>? Results { get; set; }
        }

        private sealed class VpicVehicle
        {
            public string? ErrorCode { get; set; }
            public string? ErrorText { get; set; }

            public string? ModelYear { get; set; }
            public string? Make { get; set; }
            public string? Model { get; set; }
            public string? Trim { get; set; }
            public string? Series { get; set; }

            public string? EngineModel { get; set; }
            public string? EngineCylinders { get; set; }
            public string? DisplacementL { get; set; }

            public string? TransmissionStyle { get; set; }
            public string? DriveType { get; set; }

            public string? BodyClass { get; set; }
            public string? VehicleType { get; set; }

            public string? FuelTypePrimary { get; set; }

            public string? Manufacturer { get; set; }

            public string? PlantCountry { get; set; }
            public string? PlantState { get; set; }
            public string? PlantCity { get; set; }

            public string? BasePrice { get; set; }
        }
    }
}
