using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public sealed class MarketValueService : IMarketValueService
    {
        private const string Endpoint =
            "https://api.vehicles.dev/v1/vehicles/market-value";

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public MarketValueService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey =
                configuration["Vehicles:ApiKey"]
                ?? string.Empty;
        }

        public async Task<MarketValueResult> AnalyzeAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            if (!HasVehicleIdentity(vehicle))
            {
                return BuildUnavailableResult(
                    "Year, make, and model are needed before PonyUp can request a market-value estimate.");
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return BuildUnavailableResult(
                    "The live market-value service is not configured.");
            }

            string requestUri =
                BuildRequestUri(vehicle);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    requestUri);

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    _apiKey);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            HttpResponseMessage response;

            try
            {
                response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return BuildUnavailableResult(
                    "The live valuation service timed out. PonyUp left the market value blank rather than guessing.");
            }
            catch (HttpRequestException)
            {
                return BuildUnavailableResult(
                    "The live valuation service could not be reached. PonyUp left the market value blank rather than guessing.");
            }

            using (response)
            {
                if (response.StatusCode ==
                    HttpStatusCode.PaymentRequired)
                {
                    return BuildUnavailableResult(
                        "The Vehicles.dev free-call allowance is currently unavailable or exhausted. PonyUp left the market value blank.");
                }

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    return BuildUnavailableResult(
                        "The Vehicles.dev API key was rejected.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return BuildUnavailableResult(
                        $"Live market valuation was unavailable ({(int)response.StatusCode}).");
                }

                string json;

                try
                {
                    json =
                        await response.Content.ReadAsStringAsync(
                            cancellationToken);
                }
                catch
                {
                    return BuildUnavailableResult(
                        "The valuation response could not be read.");
                }

                return ParseResponse(
                    vehicle,
                    json);
            }
        }

        private static MarketValueResult ParseResponse(
            VehicleProfile vehicle,
            string json)
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                if (!root.TryGetProperty(
                        "estimateUsd",
                        out JsonElement estimateElement) ||
                    !estimateElement.TryGetDecimal(
                        out decimal estimate) ||
                    estimate <= 0)
                {
                    return BuildUnavailableResult(
                        "The live valuation service returned no usable market-value estimate.");
                }

                decimal medianApePct = 0m;

                if (root.TryGetProperty(
                        "medianApePct",
                        out JsonElement errorElement))
                {
                    errorElement.TryGetDecimal(
                        out medianApePct);
                }

                string source =
                    "Vehicles.dev";

                if (root.TryGetProperty(
                        "source",
                        out JsonElement sourceElement) &&
                    sourceElement.ValueKind ==
                        JsonValueKind.String)
                {
                    string? returnedSource =
                        sourceElement.GetString();

                    if (!string.IsNullOrWhiteSpace(
                            returnedSource))
                    {
                        source =
                            returnedSource;
                    }
                }

                decimal marketValue =
                    RoundCurrency(estimate);

                int evidenceConfidence =
                    CalculateEvidenceConfidence(
                        vehicle);

                var sources =
                    new List<string>
                    {
                        "Vehicles.dev live US dealer-listing valuation model"
                    };

                if (!string.IsNullOrWhiteSpace(
                        vehicle.DecodeSource))
                {
                    sources.Add(
                        vehicle.DecodeSource);
                }

                string summary =
                    $"Current modeled dealer asking-market value is approximately " +
                    $"{marketValue:C0}.";

                if (medianApePct > 0)
                {
                    summary +=
                        $" Vehicles.dev reports a model-wide median absolute " +
                        $"percentage error of {medianApePct:0.#}%.";
                }

                summary +=
                    " This is an asking-price estimate based on dealer-market data, " +
                    "not a guaranteed transaction price.";

                return new MarketValueResult
                {
                    HasEstimate = true,

                    ConfidenceScore =
                        evidenceConfidence,

                    UsesLiveMarketData = true,

                    EstimateMethod =
                        "Live dealer-market valuation",

                    EstimatedMarketValue =
                        marketValue,

                    /*
                     * PonyUp's fair asking-price suggestion
                     * is derived directly from current
                     * estimated market value.
                     */
                    SuggestedAskingPrice =
                        marketValue,

                    /*
                     * Vehicles.dev Market Value does not
                     * directly provide these categories.
                     *
                     * Leave them unset rather than inventing
                     * unsupported trade, wholesale, auction,
                     * or private-party numbers.
                     */
                    LowRetailValue = 0m,
                    HighRetailValue = 0m,
                    TradeInValue = 0m,
                    WholesaleValue = 0m,
                    PrivatePartyValue = 0m,
                    EstimatedAuctionValue = 0m,

                    MarketStrength =
                        "Live dealer asking-price model",

                    DaysOnMarket = 0,

                    Summary =
                        summary,

                    Sources =
                        sources
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .ToList()
                };
            }
            catch (JsonException)
            {
                return BuildUnavailableResult(
                    "The live valuation service returned an unreadable response.");
            }
        }

        private static string BuildRequestUri(
            VehicleProfile vehicle)
        {
            var parameters =
                new List<string>();

            AddParameter(
                parameters,
                "make",
                vehicle.Make.Trim());

            AddParameter(
                parameters,
                "model",
                vehicle.Model.Trim());

            AddParameter(
                parameters,
                "year",
                vehicle.Year!.Value.ToString(
                    CultureInfo.InvariantCulture));

            if (vehicle.CurrentMileage.HasValue &&
                vehicle.CurrentMileage.Value >= 0)
            {
                AddParameter(
                    parameters,
                    "miles",
                    vehicle.CurrentMileage.Value.ToString(
                        CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Trim))
            {
                AddParameter(
                    parameters,
                    "trim",
                    vehicle.Trim.Trim());
            }

            string drivetrain =
                NormalizeDrivetrain(
                    vehicle.Drivetrain);

            if (!string.IsNullOrWhiteSpace(
                    drivetrain))
            {
                AddParameter(
                    parameters,
                    "drivetrain",
                    drivetrain);
            }

            string fuel =
                NormalizeFuel(
                    vehicle.FuelType);

            if (!string.IsNullOrWhiteSpace(
                    fuel))
            {
                AddParameter(
                    parameters,
                    "fuel",
                    fuel);
            }

            string transmission =
                NormalizeTransmission(
                    vehicle.Transmission);

            if (!string.IsNullOrWhiteSpace(
                    transmission))
            {
                AddParameter(
                    parameters,
                    "transmission",
                    transmission);
            }

            string bodyStyle =
                NormalizeBodyStyle(
                    vehicle.BodyStyle);

            if (!string.IsNullOrWhiteSpace(
                    bodyStyle))
            {
                AddParameter(
                    parameters,
                    "body_style",
                    bodyStyle);
            }

            if (vehicle.BaseMsrp.HasValue &&
                vehicle.BaseMsrp.Value > 0)
            {
                AddParameter(
                    parameters,
                    "base_msrp",
                    Math.Round(
                            vehicle.BaseMsrp.Value,
                            0,
                            MidpointRounding.AwayFromZero)
                        .ToString(
                            "0",
                            CultureInfo.InvariantCulture));
            }

            return
                $"{Endpoint}?{string.Join("&", parameters)}";
        }

        private static void AddParameter(
            ICollection<string> parameters,
            string name,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            parameters.Add(
                $"{Uri.EscapeDataString(name)}=" +
                $"{Uri.EscapeDataString(value)}");
        }

        private static string NormalizeTransmission(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text =
                value.Trim()
                    .ToUpperInvariant();

            if (text.Contains("CVT"))
            {
                return "cvt";
            }

            if (text.Contains("MANUAL"))
            {
                return "manual";
            }

            if (text.Contains("AUTO"))
            {
                return "automatic";
            }

            return value.Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeDrivetrain(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text =
                value.Trim()
                    .ToUpperInvariant();

            if (text.Contains("ALL-WHEEL") ||
                text.Contains("ALL WHEEL") ||
                text.Contains("AWD"))
            {
                return "awd";
            }

            if (text.Contains("FRONT") ||
                text.Contains("FWD"))
            {
                return "fwd";
            }

            if (text.Contains("REAR") ||
                text.Contains("RWD"))
            {
                return "rwd";
            }

            if (text.Contains("4-WHEEL") ||
                text.Contains("4 WHEEL") ||
                text.Contains("4WD") ||
                text.Contains("4X4"))
            {
                return "4wd";
            }

            return value.Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeFuel(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text =
                value.Trim()
                    .ToUpperInvariant();

            if (text.Contains("HYBRID"))
            {
                return "hybrid";
            }

            if (text.Contains("ELECTRIC"))
            {
                return "electric";
            }

            if (text.Contains("DIESEL"))
            {
                return "diesel";
            }

            if (text.Contains("E85") ||
                text.Contains("FLEX"))
            {
                return "e85";
            }

            if (text.Contains("GASOLINE") ||
                text == "GAS")
            {
                return "gasoline";
            }

            return value.Trim()
                .ToLowerInvariant();
        }

        private static string NormalizeBodyStyle(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text =
                value.Trim()
                    .ToUpperInvariant();

            if (text.Contains("SPORT UTILITY") ||
                text.Contains("SUV"))
            {
                return "suv";
            }

            if (text.Contains("PICKUP") ||
                text.Contains("TRUCK"))
            {
                return "pickup";
            }

            if (text.Contains("SEDAN"))
            {
                return "sedan";
            }

            if (text.Contains("COUPE"))
            {
                return "coupe";
            }

            if (text.Contains("HATCHBACK"))
            {
                return "hatchback";
            }

            if (text.Contains("WAGON"))
            {
                return "wagon";
            }

            if (text.Contains("CONVERTIBLE"))
            {
                return "convertible";
            }

            if (text.Contains("MINIVAN"))
            {
                return "minivan";
            }

            if (text.Contains("VAN"))
            {
                return "van";
            }

            return value.Trim()
                .ToLowerInvariant();
        }

        private static int CalculateEvidenceConfidence(
            VehicleProfile vehicle)
        {
            /*
             * This measures PonyUp input/evidence
             * completeness.
             *
             * It is NOT Vehicles.dev's prediction
             * confidence. Their medianApePct is a
             * model-wide accuracy metric.
             */
            int confidence = 50;

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Vin))
            {
                confidence += 10;
            }

            if (vehicle.CurrentMileage.HasValue)
            {
                confidence += 15;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Trim))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Drivetrain))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Transmission))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.BodyStyle))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.FuelType))
            {
                confidence += 5;
            }

            return Math.Clamp(
                confidence,
                0,
                100);
        }

        private static bool HasVehicleIdentity(
            VehicleProfile vehicle)
        {
            return
                vehicle.Year.HasValue &&
                vehicle.Year.Value >= 1900 &&
                vehicle.Year.Value <= 2100 &&
                !string.IsNullOrWhiteSpace(
                    vehicle.Make) &&
                !string.IsNullOrWhiteSpace(
                    vehicle.Model);
        }

        private static MarketValueResult BuildUnavailableResult(
            string reason)
        {
            return new MarketValueResult
            {
                HasEstimate = false,

                ConfidenceScore = 0,

                UsesLiveMarketData = false,

                EstimateMethod =
                    "Live valuation unavailable",

                MarketStrength =
                    "Not available",

                Summary =
                    reason,

                Sources =
                    new List<string>
                    {
                        "PonyUp valuation guardrail"
                    }
            };
        }

        private static decimal RoundCurrency(
            decimal value)
        {
            return Math.Round(
                value,
                0,
                MidpointRounding.AwayFromZero);
        }
    }
}
