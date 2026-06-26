using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public class RepairCostEstimatorService
    {
        private readonly List<LocationRateProfile> _locationRates = new()
        {
            new LocationRateProfile
            {
                State = "AR",
                Region = "Arkansas / Lower-Cost Market",
                LaborRateLow = 85,
                LaborRateAverage = 110,
                LaborRateHigh = 145,
                SalesTaxRate = 0.065m,
                ShopFeeAverage = 35
            },
            new LocationRateProfile
            {
                State = "MO",
                Region = "Missouri / Midwest Market",
                LaborRateLow = 90,
                LaborRateAverage = 120,
                LaborRateHigh = 155,
                SalesTaxRate = 0.04225m,
                ShopFeeAverage = 40
            },
            new LocationRateProfile
            {
                State = "TX",
                Region = "Texas / Southern Market",
                LaborRateLow = 100,
                LaborRateAverage = 135,
                LaborRateHigh = 175,
                SalesTaxRate = 0.0625m,
                ShopFeeAverage = 45
            },
            new LocationRateProfile
            {
                State = "CA",
                Region = "California / High-Cost Market",
                LaborRateLow = 145,
                LaborRateAverage = 185,
                LaborRateHigh = 245,
                SalesTaxRate = 0.0725m,
                ShopFeeAverage = 65
            },
            new LocationRateProfile
            {
                State = "DEFAULT",
                Region = "National Average",
                LaborRateLow = 95,
                LaborRateAverage = 130,
                LaborRateHigh = 175,
                SalesTaxRate = 0.06m,
                ShopFeeAverage = 45
            }
        };

        private readonly List<RepairCostProfile> _repairProfiles = new()
        {
            new RepairCostProfile
            {
                RepairType = "Brake Pads and Rotors",
                LaborHoursLow = 1.5m,
                LaborHoursAverage = 2.2m,
                LaborHoursHigh = 3.0m,
                PartsLow = 160,
                PartsAverage = 320,
                PartsHigh = 600
            },
            new RepairCostProfile
            {
                RepairType = "Alternator Replacement",
                LaborHoursLow = 1.0m,
                LaborHoursAverage = 1.7m,
                LaborHoursHigh = 2.8m,
                PartsLow = 140,
                PartsAverage = 280,
                PartsHigh = 520
            },
            new RepairCostProfile
            {
                RepairType = "Starter Replacement",
                LaborHoursLow = 1.2m,
                LaborHoursAverage = 2.0m,
                LaborHoursHigh = 3.2m,
                PartsLow = 130,
                PartsAverage = 260,
                PartsHigh = 500
            },
            new RepairCostProfile
            {
                RepairType = "Radiator Replacement",
                LaborHoursLow = 1.8m,
                LaborHoursAverage = 2.8m,
                LaborHoursHigh = 4.2m,
                PartsLow = 180,
                PartsAverage = 360,
                PartsHigh = 700
            },
            new RepairCostProfile
            {
                RepairType = "Water Pump Replacement",
                LaborHoursLow = 2.0m,
                LaborHoursAverage = 3.5m,
                LaborHoursHigh = 5.5m,
                PartsLow = 90,
                PartsAverage = 220,
                PartsHigh = 480
            },
            new RepairCostProfile
            {
                RepairType = "Transmission Replacement",
                LaborHoursLow = 6.0m,
                LaborHoursAverage = 9.0m,
                LaborHoursHigh = 13.0m,
                PartsLow = 1800,
                PartsAverage = 3200,
                PartsHigh = 5500
            },
            new RepairCostProfile
            {
                RepairType = "Engine Replacement",
                LaborHoursLow = 12.0m,
                LaborHoursAverage = 18.0m,
                LaborHoursHigh = 28.0m,
                PartsLow = 2500,
                PartsAverage = 5200,
                PartsHigh = 9500
            },
            new RepairCostProfile
            {
                RepairType = "AC Compressor Replacement",
                LaborHoursLow = 2.0m,
                LaborHoursAverage = 3.5m,
                LaborHoursHigh = 5.5m,
                PartsLow = 250,
                PartsAverage = 550,
                PartsHigh = 1100
            }
        };

        public RepairCostEstimateResult Estimate(RepairCostEstimateInput input)
        {
            LocationRateProfile location = GetLocationRate(input.State);
            RepairCostProfile repair = GetRepairProfile(input.RepairType);

            decimal diagnosticFee = input.IncludeDiagnosticFee ? 125 : 0;
            decimal towFee = input.IncludeTowFee ? 125 : 0;

            decimal lowSubtotal =
                repair.PartsLow +
                repair.LaborHoursLow * location.LaborRateLow +
                location.ShopFeeAverage +
                diagnosticFee +
                towFee;

            decimal expectedSubtotal =
                repair.PartsAverage +
                repair.LaborHoursAverage * location.LaborRateAverage +
                location.ShopFeeAverage +
                diagnosticFee +
                towFee;

            decimal highSubtotal =
                repair.PartsHigh +
                repair.LaborHoursHigh * location.LaborRateHigh +
                location.ShopFeeAverage +
                diagnosticFee +
                towFee;

            decimal lowTax = repair.PartsLow * location.SalesTaxRate;
            decimal expectedTax = repair.PartsAverage * location.SalesTaxRate;
            decimal highTax = repair.PartsHigh * location.SalesTaxRate;

            return new RepairCostEstimateResult
            {
                RepairType = repair.RepairType,
                LocationUsed = location.Region,

                LaborRateLow = location.LaborRateLow,
                LaborRateAverage = location.LaborRateAverage,
                LaborRateHigh = location.LaborRateHigh,

                LaborHoursLow = repair.LaborHoursLow,
                LaborHoursAverage = repair.LaborHoursAverage,
                LaborHoursHigh = repair.LaborHoursHigh,

                PartsLow = repair.PartsLow,
                PartsAverage = repair.PartsAverage,
                PartsHigh = repair.PartsHigh,

                ShopFeeAverage = location.ShopFeeAverage,
                SalesTaxRate = location.SalesTaxRate,

                DiagnosticFee = diagnosticFee,
                TowFee = towFee,

                LowEstimate = Math.Round(lowSubtotal + lowTax, 2),
                ExpectedEstimate = Math.Round(expectedSubtotal + expectedTax, 2),
                HighEstimate = Math.Round(highSubtotal + highTax, 2),

                EstimateNote =
                    "This is a location-adjusted estimate using seeded labor rate and repair profile averages. The user can override the estimate with a real shop quote."
            };
        }

        private LocationRateProfile GetLocationRate(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return _locationRates.First(x => x.State == "DEFAULT");

            string normalized = state.Trim().ToUpperInvariant();

            return _locationRates.FirstOrDefault(x => x.State == normalized)
                   ?? _locationRates.First(x => x.State == "DEFAULT");
        }

        private RepairCostProfile GetRepairProfile(string repairType)
        {
            if (string.IsNullOrWhiteSpace(repairType))
                return _repairProfiles.First();

            return _repairProfiles.FirstOrDefault(x =>
                       x.RepairType.Equals(repairType.Trim(), StringComparison.OrdinalIgnoreCase))
                   ?? _repairProfiles.First();
        }

        public List<string> GetRepairTypes()
        {
            return _repairProfiles
                .Select(x => x.RepairType)
                .OrderBy(x => x)
                .ToList();
        }
    }
}