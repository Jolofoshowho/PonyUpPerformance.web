using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public class RepairCostEstimatorService
    {
        private const decimal NationalLaborRateLow = 95m;
        private const decimal NationalLaborRateAverage = 130m;
        private const decimal NationalLaborRateHigh = 175m;
        private const decimal NationalShopFeeAverage = 45m;
        private const decimal DefaultSalesTaxRate = 0.06m;

        private static readonly Dictionary<string, string> StateNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AL"] = "Alabama",
                ["AK"] = "Alaska",
                ["AZ"] = "Arizona",
                ["AR"] = "Arkansas",
                ["CA"] = "California",
                ["CO"] = "Colorado",
                ["CT"] = "Connecticut",
                ["DE"] = "Delaware",
                ["FL"] = "Florida",
                ["GA"] = "Georgia",
                ["HI"] = "Hawaii",
                ["ID"] = "Idaho",
                ["IL"] = "Illinois",
                ["IN"] = "Indiana",
                ["IA"] = "Iowa",
                ["KS"] = "Kansas",
                ["KY"] = "Kentucky",
                ["LA"] = "Louisiana",
                ["ME"] = "Maine",
                ["MD"] = "Maryland",
                ["MA"] = "Massachusetts",
                ["MI"] = "Michigan",
                ["MN"] = "Minnesota",
                ["MS"] = "Mississippi",
                ["MO"] = "Missouri",
                ["MT"] = "Montana",
                ["NE"] = "Nebraska",
                ["NV"] = "Nevada",
                ["NH"] = "New Hampshire",
                ["NJ"] = "New Jersey",
                ["NM"] = "New Mexico",
                ["NY"] = "New York",
                ["NC"] = "North Carolina",
                ["ND"] = "North Dakota",
                ["OH"] = "Ohio",
                ["OK"] = "Oklahoma",
                ["OR"] = "Oregon",
                ["PA"] = "Pennsylvania",
                ["RI"] = "Rhode Island",
                ["SC"] = "South Carolina",
                ["SD"] = "South Dakota",
                ["TN"] = "Tennessee",
                ["TX"] = "Texas",
                ["UT"] = "Utah",
                ["VT"] = "Vermont",
                ["VA"] = "Virginia",
                ["WA"] = "Washington",
                ["WV"] = "West Virginia",
                ["WI"] = "Wisconsin",
                ["WY"] = "Wyoming",
                ["DC"] = "District of Columbia"
            };

        private static readonly Dictionary<string, decimal> StateCostMultipliers =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AL"] = 0.88m,
                ["AK"] = 1.20m,
                ["AZ"] = 1.02m,
                ["AR"] = 0.85m,
                ["CA"] = 1.42m,
                ["CO"] = 1.15m,
                ["CT"] = 1.28m,
                ["DE"] = 1.08m,
                ["FL"] = 1.05m,
                ["GA"] = 0.96m,
                ["HI"] = 1.35m,
                ["ID"] = 0.96m,
                ["IL"] = 1.08m,
                ["IN"] = 0.92m,
                ["IA"] = 0.90m,
                ["KS"] = 0.90m,
                ["KY"] = 0.89m,
                ["LA"] = 0.93m,
                ["ME"] = 1.04m,
                ["MD"] = 1.20m,
                ["MA"] = 1.34m,
                ["MI"] = 1.00m,
                ["MN"] = 1.08m,
                ["MS"] = 0.84m,
                ["MO"] = 0.92m,
                ["MT"] = 0.98m,
                ["NE"] = 0.92m,
                ["NV"] = 1.14m,
                ["NH"] = 1.18m,
                ["NJ"] = 1.30m,
                ["NM"] = 0.92m,
                ["NY"] = 1.38m,
                ["NC"] = 0.95m,
                ["ND"] = 1.02m,
                ["OH"] = 0.95m,
                ["OK"] = 0.88m,
                ["OR"] = 1.18m,
                ["PA"] = 1.04m,
                ["RI"] = 1.20m,
                ["SC"] = 0.92m,
                ["SD"] = 0.92m,
                ["TN"] = 0.90m,
                ["TX"] = 1.04m,
                ["UT"] = 1.02m,
                ["VT"] = 1.10m,
                ["VA"] = 1.08m,
                ["WA"] = 1.28m,
                ["WV"] = 0.88m,
                ["WI"] = 1.00m,
                ["WY"] = 1.00m,
                ["DC"] = 1.40m
            };

        private static readonly Dictionary<string, decimal> StateSalesTaxRates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AL"] = 0.04m,
                ["AK"] = 0.00m,
                ["AZ"] = 0.056m,
                ["AR"] = 0.065m,
                ["CA"] = 0.0725m,
                ["CO"] = 0.029m,
                ["CT"] = 0.0635m,
                ["DE"] = 0.00m,
                ["FL"] = 0.06m,
                ["GA"] = 0.04m,
                ["HI"] = 0.04m,
                ["ID"] = 0.06m,
                ["IL"] = 0.0625m,
                ["IN"] = 0.07m,
                ["IA"] = 0.06m,
                ["KS"] = 0.065m,
                ["KY"] = 0.06m,
                ["LA"] = 0.0445m,
                ["ME"] = 0.055m,
                ["MD"] = 0.06m,
                ["MA"] = 0.0625m,
                ["MI"] = 0.06m,
                ["MN"] = 0.06875m,
                ["MS"] = 0.07m,
                ["MO"] = 0.04225m,
                ["MT"] = 0.00m,
                ["NE"] = 0.055m,
                ["NV"] = 0.0685m,
                ["NH"] = 0.00m,
                ["NJ"] = 0.06625m,
                ["NM"] = 0.05125m,
                ["NY"] = 0.04m,
                ["NC"] = 0.0475m,
                ["ND"] = 0.05m,
                ["OH"] = 0.0575m,
                ["OK"] = 0.045m,
                ["OR"] = 0.00m,
                ["PA"] = 0.06m,
                ["RI"] = 0.07m,
                ["SC"] = 0.06m,
                ["SD"] = 0.042m,
                ["TN"] = 0.07m,
                ["TX"] = 0.0625m,
                ["UT"] = 0.0485m,
                ["VT"] = 0.06m,
                ["VA"] = 0.053m,
                ["WA"] = 0.065m,
                ["WV"] = 0.06m,
                ["WI"] = 0.05m,
                ["WY"] = 0.04m,
                ["DC"] = 0.06m
            };

        private readonly List<RepairCostProfile> _repairProfiles = new()
        {
            new RepairCostProfile
            {
                RepairType = "Brake Pads and Rotors",
                LaborHoursLow = 1.5m,
                LaborHoursAverage = 2.2m,
                LaborHoursHigh = 3.0m,
                PartsLow = 160m,
                PartsAverage = 320m,
                PartsHigh = 600m
            },
            new RepairCostProfile
            {
                RepairType = "Alternator Replacement",
                LaborHoursLow = 1.0m,
                LaborHoursAverage = 1.7m,
                LaborHoursHigh = 2.8m,
                PartsLow = 140m,
                PartsAverage = 280m,
                PartsHigh = 520m
            },
            new RepairCostProfile
            {
                RepairType = "Starter Replacement",
                LaborHoursLow = 1.2m,
                LaborHoursAverage = 2.0m,
                LaborHoursHigh = 3.2m,
                PartsLow = 130m,
                PartsAverage = 260m,
                PartsHigh = 500m
            },
            new RepairCostProfile
            {
                RepairType = "Radiator Replacement",
                LaborHoursLow = 1.8m,
                LaborHoursAverage = 2.8m,
                LaborHoursHigh = 4.2m,
                PartsLow = 180m,
                PartsAverage = 360m,
                PartsHigh = 700m
            },
            new RepairCostProfile
            {
                RepairType = "Water Pump Replacement",
                LaborHoursLow = 2.0m,
                LaborHoursAverage = 3.5m,
                LaborHoursHigh = 5.5m,
                PartsLow = 90m,
                PartsAverage = 220m,
                PartsHigh = 480m
            },
            new RepairCostProfile
            {
                RepairType = "Transmission Replacement",
                LaborHoursLow = 6.0m,
                LaborHoursAverage = 9.0m,
                LaborHoursHigh = 13.0m,
                PartsLow = 1800m,
                PartsAverage = 3200m,
                PartsHigh = 5500m
            },
            new RepairCostProfile
            {
                RepairType = "Engine Replacement",
                LaborHoursLow = 12.0m,
                LaborHoursAverage = 18.0m,
                LaborHoursHigh = 28.0m,
                PartsLow = 2500m,
                PartsAverage = 5200m,
                PartsHigh = 9500m
            },
            new RepairCostProfile
            {
                RepairType = "AC Compressor Replacement",
                LaborHoursLow = 2.0m,
                LaborHoursAverage = 3.5m,
                LaborHoursHigh = 5.5m,
                PartsLow = 250m,
                PartsAverage = 550m,
                PartsHigh = 1100m
            }
        };

        public RepairCostEstimateResult Estimate(RepairCostEstimateInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            LocationRateProfile location = GetLocationRate(input);
            RepairCostProfile repair = GetRepairProfile(input.RepairType);

            decimal diagnosticFee =
                input.IncludeDiagnosticFee ? 125m : 0m;

            decimal towFee =
                input.IncludeTowFee ? 125m : 0m;

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

            decimal lowTax =
                repair.PartsLow * location.SalesTaxRate;

            decimal expectedTax =
                repair.PartsAverage * location.SalesTaxRate;

            decimal highTax =
                repair.PartsHigh * location.SalesTaxRate;

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

                LowEstimate =
                    Math.Round(lowSubtotal + lowTax, 2),

                ExpectedEstimate =
                    Math.Round(expectedSubtotal + expectedTax, 2),

                HighEstimate =
                    Math.Round(highSubtotal + highTax, 2),

                EstimateNote =
                    "This estimate uses manually entered location information, seeded state-level labor rates, repair-time averages, parts averages, shop fees, and estimated sales tax. Actual shop pricing may vary, and the estimated repair cost can be replaced with a written shop quote."
            };
        }

        public List<string> GetRepairTypes()
        {
            return _repairProfiles
                .Select(x => x.RepairType)
                .OrderBy(x => x)
                .ToList();
        }

        private static LocationRateProfile GetLocationRate(
            RepairCostEstimateInput input)
        {
            string stateCode = NormalizeState(input.State);

            if (string.IsNullOrWhiteSpace(stateCode))
            {
                stateCode = TryResolveStateFromZip(input.ZipCode);
            }

            if (string.IsNullOrWhiteSpace(stateCode) ||
                !StateNames.TryGetValue(stateCode, out string? stateName))
            {
                return new LocationRateProfile
                {
                    State = "DEFAULT",
                    Region = BuildNationalLocationLabel(input),
                    LaborRateLow = NationalLaborRateLow,
                    LaborRateAverage = NationalLaborRateAverage,
                    LaborRateHigh = NationalLaborRateHigh,
                    SalesTaxRate = DefaultSalesTaxRate,
                    ShopFeeAverage = NationalShopFeeAverage
                };
            }

            decimal stateMultiplier =
                StateCostMultipliers.TryGetValue(
                    stateCode,
                    out decimal configuredMultiplier)
                    ? configuredMultiplier
                    : 1.00m;

            decimal zipMultiplier =
                GetZipCostMultiplier(input.ZipCode);

            decimal combinedMultiplier =
                Math.Clamp(
                    stateMultiplier * zipMultiplier,
                    0.75m,
                    1.60m);

            decimal salesTaxRate =
                StateSalesTaxRates.TryGetValue(
                    stateCode,
                    out decimal configuredTaxRate)
                    ? configuredTaxRate
                    : DefaultSalesTaxRate;

            return new LocationRateProfile
            {
                State = stateCode,
                Region = BuildLocationLabel(
                    input.City,
                    stateCode,
                    input.ZipCode,
                    stateName),

                LaborRateLow =
                    RoundCurrency(
                        NationalLaborRateLow * combinedMultiplier),

                LaborRateAverage =
                    RoundCurrency(
                        NationalLaborRateAverage * combinedMultiplier),

                LaborRateHigh =
                    RoundCurrency(
                        NationalLaborRateHigh * combinedMultiplier),

                SalesTaxRate = salesTaxRate,

                ShopFeeAverage =
                    RoundCurrency(
                        NationalShopFeeAverage * combinedMultiplier)
            };
        }

        private RepairCostProfile GetRepairProfile(string repairType)
        {
            if (string.IsNullOrWhiteSpace(repairType))
            {
                return _repairProfiles.First();
            }

            return _repairProfiles.FirstOrDefault(x =>
                       x.RepairType.Equals(
                           repairType.Trim(),
                           StringComparison.OrdinalIgnoreCase))
                   ?? _repairProfiles.First();
        }

        private static string NormalizeState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return string.Empty;
            }

            string normalized = state
                .Trim()
                .Replace(".", string.Empty)
                .ToUpperInvariant();

            if (normalized.Length == 2 &&
                StateNames.ContainsKey(normalized))
            {
                return normalized;
            }

            foreach (KeyValuePair<string, string> entry in StateNames)
            {
                if (entry.Value.Equals(
                        state.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Key;
                }
            }

            return string.Empty;
        }

        private static string TryResolveStateFromZip(string zipCode)
        {
            string digits = GetZipDigits(zipCode);

            if (digits.Length < 3 ||
                !int.TryParse(digits[..3], out int prefix))
            {
                return string.Empty;
            }

            return prefix switch
            {
                >= 5 and <= 5 => "NY",
                >= 6 and <= 9 => "PR",
                >= 10 and <= 27 => "MA",
                >= 28 and <= 29 => "RI",
                >= 30 and <= 38 => "NH",
                >= 39 and <= 49 => "ME",
                >= 50 and <= 59 => "VT",
                >= 60 and <= 69 => "CT",
                >= 70 and <= 89 => "NJ",
                >= 100 and <= 149 => "NY",
                >= 150 and <= 196 => "PA",
                >= 197 and <= 199 => "DE",
                >= 200 and <= 205 => "DC",
                >= 206 and <= 219 => "MD",
                >= 220 and <= 246 => "VA",
                >= 247 and <= 268 => "WV",
                >= 270 and <= 289 => "NC",
                >= 290 and <= 299 => "SC",
                >= 300 and <= 319 => "GA",
                >= 320 and <= 349 => "FL",
                >= 350 and <= 369 => "AL",
                >= 370 and <= 385 => "TN",
                >= 386 and <= 397 => "MS",
                >= 398 and <= 399 => "GA",
                >= 400 and <= 427 => "KY",
                >= 430 and <= 459 => "OH",
                >= 460 and <= 479 => "IN",
                >= 480 and <= 499 => "MI",
                >= 500 and <= 528 => "IA",
                >= 530 and <= 549 => "WI",
                >= 550 and <= 567 => "MN",
                >= 570 and <= 577 => "SD",
                >= 580 and <= 588 => "ND",
                >= 590 and <= 599 => "MT",
                >= 600 and <= 629 => "IL",
                >= 630 and <= 658 => "MO",
                >= 660 and <= 679 => "KS",
                >= 680 and <= 693 => "NE",
                >= 700 and <= 714 => "LA",
                >= 716 and <= 729 => "AR",
                >= 730 and <= 749 => "OK",
                >= 750 and <= 799 => "TX",
                >= 800 and <= 816 => "CO",
                >= 820 and <= 831 => "WY",
                >= 832 and <= 838 => "ID",
                >= 840 and <= 847 => "UT",
                >= 850 and <= 865 => "AZ",
                >= 870 and <= 884 => "NM",
                >= 889 and <= 898 => "NV",
                >= 900 and <= 961 => "CA",
                >= 967 and <= 968 => "HI",
                >= 970 and <= 979 => "OR",
                >= 980 and <= 994 => "WA",
                >= 995 and <= 999 => "AK",
                _ => string.Empty
            };
        }

        private static decimal GetZipCostMultiplier(string zipCode)
        {
            string digits = GetZipDigits(zipCode);

            if (digits.Length < 5)
            {
                return 1.00m;
            }

            char firstDigit = digits[0];

            return firstDigit switch
            {
                '0' => 1.04m,
                '1' => 1.08m,
                '2' => 1.03m,
                '3' => 0.98m,
                '4' => 0.96m,
                '5' => 0.95m,
                '6' => 0.96m,
                '7' => 0.96m,
                '8' => 1.01m,
                '9' => 1.10m,
                _ => 1.00m
            };
        }

        private static string BuildLocationLabel(
            string city,
            string stateCode,
            string zipCode,
            string stateName)
        {
            string cleanCity =
                string.IsNullOrWhiteSpace(city)
                    ? string.Empty
                    : city.Trim();

            string cleanZip = GetDisplayZip(zipCode);

            if (!string.IsNullOrWhiteSpace(cleanCity) &&
                !string.IsNullOrWhiteSpace(cleanZip))
            {
                return $"{cleanCity}, {stateCode} {cleanZip}";
            }

            if (!string.IsNullOrWhiteSpace(cleanCity))
            {
                return $"{cleanCity}, {stateCode}";
            }

            if (!string.IsNullOrWhiteSpace(cleanZip))
            {
                return $"{stateName} ZIP {cleanZip}";
            }

            return $"{stateName} state average";
        }

        private static string BuildNationalLocationLabel(
            RepairCostEstimateInput input)
        {
            string cleanCity =
                string.IsNullOrWhiteSpace(input.City)
                    ? string.Empty
                    : input.City.Trim();

            string cleanState =
                string.IsNullOrWhiteSpace(input.State)
                    ? string.Empty
                    : input.State.Trim();

            string cleanZip =
                GetDisplayZip(input.ZipCode);

            List<string> providedLocationParts = new();

            if (!string.IsNullOrWhiteSpace(cleanCity))
            {
                providedLocationParts.Add(cleanCity);
            }

            if (!string.IsNullOrWhiteSpace(cleanState))
            {
                providedLocationParts.Add(cleanState);
            }

            if (!string.IsNullOrWhiteSpace(cleanZip))
            {
                providedLocationParts.Add(cleanZip);
            }

            if (providedLocationParts.Count == 0)
            {
                return "National Average";
            }

            return
                $"{string.Join(", ", providedLocationParts)} — National Average fallback";
        }

        private static string GetZipDigits(string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                return string.Empty;
            }

            return new string(
                zipCode
                    .Where(char.IsDigit)
                    .Take(9)
                    .ToArray());
        }

        private static string GetDisplayZip(string zipCode)
        {
            string digits = GetZipDigits(zipCode);

            if (digits.Length < 5)
            {
                return string.Empty;
            }

            return digits[..5];
        }

        private static decimal RoundCurrency(decimal value)
        {
            return Math.Round(value, 2);
        }
    }
}
