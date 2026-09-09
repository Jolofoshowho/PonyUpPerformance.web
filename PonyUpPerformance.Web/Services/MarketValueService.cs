using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public sealed class MarketValueService
        : IMarketValueService
    {
        public Task<MarketValueResult> AnalyzeAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            cancellationToken.ThrowIfCancellationRequested();

            if (!HasVehicleIdentity(vehicle))
            {
                return Task.FromResult(
                    BuildUnavailableResult(
                        "Vehicle identity is incomplete, so PonyUp cannot produce a responsible market-value estimate."));
            }

            /*
             * For this first $0 valuation engine, PonyUp
             * requires a usable original MSRP anchor.
             *
             * We deliberately refuse to invent a dollar
             * value when no defensible price anchor exists.
             *
             * Later, live comparable-market data can become
             * another IMarketValueService implementation
             * without changing the Buy Analyzer architecture.
             */
            if (!vehicle.BaseMsrp.HasValue ||
                vehicle.BaseMsrp.Value <= 0)
            {
                return Task.FromResult(
                    BuildUnavailableResult(
                        "PonyUp decoded the vehicle, but no usable original MSRP was available. " +
                        "A modeled market value was not invented."));
            }

            int currentYear =
                DateTime.UtcNow.Year;

            int vehicleAge =
                Math.Clamp(
                    currentYear -
                    vehicle.Year!.Value,
                    0,
                    60);

            decimal fiveYearRetention =
                DetermineFiveYearRetention(vehicle);

            decimal longTermFloor =
                DetermineLongTermRetentionFloor(vehicle);

            decimal ageRetention =
                CalculateAgeRetention(
                    vehicleAge,
                    fiveYearRetention,
                    longTermFloor);

            decimal mileageMultiplier =
                CalculateMileageMultiplier(
                    vehicleAge,
                    vehicle.CurrentMileage);

            decimal conditionMultiplier =
                CalculateConditionMultiplier(
                    vehicle.MechanicalCondition);

            decimal titleMultiplier =
                CalculateTitleMultiplier(
                    vehicle.TitleStatus);

            decimal accidentMultiplier =
                CalculateAccidentMultiplier(
                    vehicle.AccidentHistory);

            decimal modeledValue =
                vehicle.BaseMsrp.Value *
                ageRetention *
                mileageMultiplier *
                conditionMultiplier *
                titleMultiplier *
                accidentMultiplier;

            modeledValue =
                Math.Max(
                    0m,
                    modeledValue);

            int confidence =
                CalculateConfidence(
                    vehicle,
                    vehicleAge);

            decimal rangePercent =
                confidence switch
                {
                    >= 70 => 0.10m,
                    >= 55 => 0.15m,
                    _ => 0.20m
                };

            decimal estimatedMarketValue =
                RoundMarketValue(
                    modeledValue);

            decimal privatePartyValue =
                estimatedMarketValue;

            decimal suggestedAskingPrice =
                privatePartyValue;

            decimal lowRetailValue =
                RoundMarketValue(
                    estimatedMarketValue *
                    (1m - rangePercent));

            decimal highRetailValue =
                RoundMarketValue(
                    estimatedMarketValue *
                    (1m + rangePercent));

            decimal tradeInValue =
                RoundMarketValue(
                    estimatedMarketValue * 0.80m);

            decimal wholesaleValue =
                RoundMarketValue(
                    estimatedMarketValue * 0.72m);

            decimal auctionValue =
                RoundMarketValue(
                    estimatedMarketValue * 0.66m);

            var sources =
                BuildSources(vehicle);

            string summary =
                BuildSummary(
                    vehicle,
                    vehicleAge,
                    confidence,
                    estimatedMarketValue,
                    lowRetailValue,
                    highRetailValue);

            return Task.FromResult(
                new MarketValueResult
                {
                    HasEstimate = true,

                    ConfidenceScore = confidence,

                    UsesLiveMarketData = false,

                    EstimateMethod =
                        "PonyUp modeled valuation",

                    EstimatedMarketValue =
                        estimatedMarketValue,

                    SuggestedAskingPrice =
                        suggestedAskingPrice,

                    LowRetailValue =
                        lowRetailValue,

                    HighRetailValue =
                        highRetailValue,

                    TradeInValue =
                        tradeInValue,

                    WholesaleValue =
                        wholesaleValue,

                    PrivatePartyValue =
                        privatePartyValue,

                    EstimatedAuctionValue =
                        auctionValue,

                    MarketStrength =
                        "Not measured — live market not checked",

                    DaysOnMarket = 0,

                    Summary = summary,

                    Sources = sources
                });
        }

        private static bool HasVehicleIdentity(
            VehicleProfile vehicle)
        {
            return
                vehicle.Year.HasValue &&
                !string.IsNullOrWhiteSpace(vehicle.Make) &&
                !string.IsNullOrWhiteSpace(vehicle.Model);
        }

        /*
         * Current segment-level five-year retention anchors.
         *
         * These are intentionally broad market patterns,
         * not make/model-specific valuations.
         */
        private static decimal DetermineFiveYearRetention(
            VehicleProfile vehicle)
        {
            string fuel =
                vehicle.FuelType
                    .Trim()
                    .ToUpperInvariant();

            string body =
                $"{vehicle.BodyStyle} {vehicle.VehicleType}"
                    .Trim()
                    .ToUpperInvariant();

            if (fuel.Contains("ELECTRIC") &&
                !fuel.Contains("HYBRID"))
            {
                return 0.428m;
            }

            if (fuel.Contains("HYBRID"))
            {
                return 0.646m;
            }

            if (body.Contains("PICKUP") ||
                body.Contains("TRUCK"))
            {
                return 0.658m;
            }

            if (body.Contains("SUV") ||
                body.Contains("SPORT UTILITY"))
            {
                return 0.551m;
            }

            return 0.582m;
        }

        private static decimal DetermineLongTermRetentionFloor(
            VehicleProfile vehicle)
        {
            string fuel =
                vehicle.FuelType
                    .Trim()
                    .ToUpperInvariant();

            string body =
                $"{vehicle.BodyStyle} {vehicle.VehicleType}"
                    .Trim()
                    .ToUpperInvariant();

            if (fuel.Contains("ELECTRIC") &&
                !fuel.Contains("HYBRID"))
            {
                return 0.06m;
            }

            if (body.Contains("PICKUP") ||
                body.Contains("TRUCK"))
            {
                return 0.15m;
            }

            return 0.12m;
        }

        private static decimal CalculateAgeRetention(
            int age,
            decimal fiveYearRetention,
            decimal longTermFloor)
        {
            /*
             * Current-model-year used vehicle:
             * assume some immediate used-market depreciation.
             */
            if (age <= 0)
            {
                return 0.90m;
            }

            /*
             * First year contains the steepest normal
             * depreciation step.
             */
            if (age == 1)
            {
                return 0.80m;
            }

            /*
             * Years 2-5 transition smoothly from the
             * first-year residual to the five-year
             * segment retention anchor.
             */
            if (age <= 5)
            {
                double progress =
                    (age - 1) / 4.0;

                double ratio =
                    (double)(
                        fiveYearRetention /
                        0.80m);

                decimal retention =
                    0.80m *
                    (decimal)Math.Pow(
                        ratio,
                        progress);

                return Math.Max(
                    longTermFloor,
                    retention);
            }

            /*
             * After year five, use gradual depreciation
             * until the long-term residual floor is reached.
             *
             * This is deliberately conservative for older
             * vehicles because collector/special-interest
             * pricing cannot be inferred safely from VIN
             * specifications alone.
             */
            decimal laterRetention =
                fiveYearRetention *
                (decimal)Math.Pow(
                    0.90,
                    age - 5);

            return Math.Max(
                longTermFloor,
                laterRetention);
        }

        private static decimal CalculateMileageMultiplier(
            int vehicleAge,
            int? mileage)
        {
            if (!mileage.HasValue ||
                mileage.Value < 0)
            {
                return 1m;
            }

            decimal expectedMileage =
                Math.Max(
                    12_000m,
                    Math.Max(
                        1,
                        vehicleAge) *
                    12_000m);

            decimal mileageRatio =
                mileage.Value /
                expectedMileage;

            return mileageRatio switch
            {
                <= 0.50m => 1.20m,
                <= 0.75m => 1.12m,
                <= 0.90m => 1.06m,
                <= 1.10m => 1.00m,
                <= 1.25m => 0.94m,
                <= 1.50m => 0.88m,
                <= 2.00m => 0.80m,
                _ => 0.70m
            };
        }

        private static decimal CalculateConditionMultiplier(
            MechanicalCondition condition)
        {
            return condition switch
            {
                MechanicalCondition.Excellent => 1.12m,
                MechanicalCondition.Good => 1.05m,
                MechanicalCondition.Fair => 0.90m,
                MechanicalCondition.Poor => 0.70m,
                MechanicalCondition.Severe => 0.45m,
                _ => 1.00m
            };
        }

        private static decimal CalculateTitleMultiplier(
            TitleStatus titleStatus)
        {
            return titleStatus switch
            {
                TitleStatus.Clean => 1.00m,
                TitleStatus.Rebuilt => 0.75m,
                TitleStatus.Salvage => 0.60m,
                TitleStatus.Flood => 0.50m,
                _ => 1.00m
            };
        }

        private static decimal CalculateAccidentMultiplier(
            AccidentHistory accidentHistory)
        {
            return accidentHistory switch
            {
                AccidentHistory.None => 1.00m,
                AccidentHistory.Minor => 0.95m,
                AccidentHistory.Moderate => 0.85m,
                AccidentHistory.Major => 0.70m,
                _ => 1.00m
            };
        }

        private static int CalculateConfidence(
            VehicleProfile vehicle,
            int vehicleAge)
        {
            int confidence = 25;

            if (vehicle.BaseMsrp.HasValue &&
                vehicle.BaseMsrp.Value > 0)
            {
                confidence += 25;
            }

            if (!string.IsNullOrWhiteSpace(vehicle.Vin))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(vehicle.Trim))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(vehicle.Engine))
            {
                confidence += 5;
            }

            if (vehicle.CurrentMileage.HasValue)
            {
                confidence += 10;
            }

            if (vehicle.MechanicalCondition !=
                MechanicalCondition.NotProvided)
            {
                confidence += 10;
            }

            if (vehicle.TitleStatus !=
                TitleStatus.NotProvided)
            {
                confidence += 5;
            }

            if (vehicle.AccidentHistory !=
                AccidentHistory.NotProvided)
            {
                confidence += 5;
            }

            /*
             * Older vehicles increasingly diverge because
             * collectibility, regional rust, preservation,
             * modifications and rarity matter more.
             */
            if (vehicleAge > 25)
            {
                confidence -= 20;
            }
            else if (vehicleAge > 15)
            {
                confidence -= 10;
            }

            /*
             * No live comparable listings are being used,
             * so this model intentionally cannot claim
             * very-high market-data confidence.
             */
            return Math.Clamp(
                confidence,
                0,
                80);
        }

        private static List<string> BuildSources(
            VehicleProfile vehicle)
        {
            var sources =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                    vehicle.DecodeSource))
            {
                sources.Add(
                    vehicle.DecodeSource);
            }

            sources.Add(
                "PonyUp modeled depreciation and condition analysis");

            sources.Add(
                "2026 segment-level used-vehicle retention calibration");

            return sources
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildSummary(
            VehicleProfile vehicle,
            int vehicleAge,
            int confidence,
            decimal marketValue,
            decimal lowValue,
            decimal highValue)
        {
            string vehicleName =
                string.IsNullOrWhiteSpace(
                    vehicle.DisplayName)
                    ? "vehicle"
                    : vehicle.DisplayName;

            string summary =
                $"PonyUp estimates the {vehicleName} at approximately " +
                $"{marketValue:C0}, with a modeled range of " +
                $"{lowValue:C0} to {highValue:C0}. " +
                $"Estimate confidence is {confidence}/100.";

            if (!vehicle.CurrentMileage.HasValue)
            {
                summary +=
                    " Mileage was not provided, so no mileage adjustment was applied.";
            }

            if (vehicle.MechanicalCondition ==
                MechanicalCondition.NotProvided)
            {
                summary +=
                    " Mechanical condition was not provided.";
            }

            if (vehicleAge > 20)
            {
                summary +=
                    " Older and special-interest vehicles can vary materially because preservation, rarity, modifications and collector demand are not captured by this model.";
            }

            summary +=
                " This is a PonyUp modeled estimate and does not yet use live comparable listings.";

            return summary;
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
                    "Insufficient valuation evidence",

                MarketStrength =
                    "Not measured",

                Summary = reason,

                Sources = new List<string>
                {
                    "PonyUp valuation guardrail"
                }
            };
        }

        private static decimal RoundMarketValue(
            decimal value)
        {
            if (value <= 0)
            {
                return 0m;
            }

            /*
             * Modeled estimates should not imply false
             * penny-level precision.
             */
            return Math.Round(
                value / 50m,
                0,
                MidpointRounding.AwayFromZero) * 50m;
        }
    }
}
