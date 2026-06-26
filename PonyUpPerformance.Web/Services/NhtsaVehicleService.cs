using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public class NhtsaVehicleService
    {
        private readonly HttpClient _httpClient;

        public NhtsaVehicleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<NhtsaVinDecodeResult> DecodeVinAsync(string vin)
        {
            vin = (vin ?? "").Trim().ToUpperInvariant();

            if (vin.Length != 17)
            {
                return new NhtsaVinDecodeResult
                {
                    Success = false,
                    Message = "VIN must be 17 characters."
                };
            }

            string url = $"https://vpic.nhtsa.dot.gov/api/vehicles/DecodeVinValues/{vin}?format=json";

            NhtsaVinResponse? response = await _httpClient.GetFromJsonAsync<NhtsaVinResponse>(url);
            NhtsaVinItem? item = response?.Results?.FirstOrDefault();

            if (item == null)
            {
                return new NhtsaVinDecodeResult
                {
                    Success = false,
                    Message = "No vehicle data returned."
                };
            }

            int.TryParse(item.ModelYear, out int year);

            return new NhtsaVinDecodeResult
            {
                Success = true,
                Message = "VIN decoded.",
                Vin = vin,
                Year = year,
                Make = item.Make ?? "",
                Model = item.Model ?? "",
                Trim = item.Trim ?? "",
                BodyStyle = NormalizeBodyStyle(item.BodyClass),
                Drivetrain = NormalizeDrivetrain(item.DriveType),
                FuelType = item.FuelTypePrimary ?? "",
                Engine = BuildEngineDescription(item)
            };
        }

        private static string BuildEngineDescription(NhtsaVinItem item)
        {
            List<string> parts = new();

            if (!string.IsNullOrWhiteSpace(item.DisplacementL))
                parts.Add(item.DisplacementL + "L");

            if (!string.IsNullOrWhiteSpace(item.EngineCylinders))
                parts.Add("V" + item.EngineCylinders);

            if (!string.IsNullOrWhiteSpace(item.EngineConfiguration))
                parts.Add(item.EngineConfiguration);

            if (!string.IsNullOrWhiteSpace(item.EngineModel))
                parts.Add(item.EngineModel);

            return string.Join(" ", parts).Trim();
        }

        private static string NormalizeBodyStyle(string? bodyClass)
        {
            string body = (bodyClass ?? "").ToLowerInvariant();

            if (body.Contains("pickup")) return "Truck";
            if (body.Contains("sport utility") || body.Contains("suv") || body.Contains("multi-purpose")) return "SUV";
            if (body.Contains("coupe")) return "Coupe";
            if (body.Contains("van")) return "Van";
            if (body.Contains("wagon")) return "Wagon";
            if (body.Contains("sedan")) return "Sedan";

            return "Sedan";
        }

        private static string NormalizeDrivetrain(string? driveType)
        {
            string drive = (driveType ?? "").ToLowerInvariant();

            if (drive.Contains("front")) return "FWD";
            if (drive.Contains("rear")) return "RWD";
            if (drive.Contains("4wd") || drive.Contains("4x4") || drive.Contains("four")) return "4WD";
            if (drive.Contains("awd") || drive.Contains("all")) return "AWD";

            return driveType ?? "";
        }

        private class NhtsaVinResponse
        {
            [JsonPropertyName("Results")]
            public List<NhtsaVinItem> Results { get; set; } = new();
        }

        private class NhtsaVinItem
        {
            public string? ModelYear { get; set; }
            public string? Make { get; set; }
            public string? Model { get; set; }
            public string? Trim { get; set; }
            public string? BodyClass { get; set; }
            public string? DriveType { get; set; }
            public string? FuelTypePrimary { get; set; }
            public string? EngineConfiguration { get; set; }
            public string? EngineCylinders { get; set; }
            public string? DisplacementL { get; set; }
            public string? EngineModel { get; set; }
        }
    }
}