using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services
{
    public sealed class FuelEconomyVehicleSpecService
        : IVehicleSpecEnrichmentService
    {
        private const string BaseUrl =
            "https://www.fueleconomy.gov/ws/rest";

        private readonly HttpClient _httpClient;

        public FuelEconomyVehicleSpecService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<VehicleProfile> EnrichAsync(
            VehicleProfile vehicle,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(vehicle);

            if (!vehicle.Year.HasValue ||
                vehicle.Year.Value < 1984 ||
                string.IsNullOrWhiteSpace(vehicle.Make) ||
                string.IsNullOrWhiteSpace(vehicle.Model))
            {
                return vehicle;
            }

            try
            {
                var options =
                    await GetOptionsAsync(
                        vehicle.Year.Value,
                        vehicle.Make,
                        vehicle.Model,
                        cancellationToken);

                if (options.Count == 0)
                {
                    return vehicle;
                }

                var rankedOptions =
                    options
                        .Select(option => new
                        {
                            Option = option,
                            Score = ScoreOptionText(
                                vehicle,
                                option.Text)
                        })
                        .OrderByDescending(x => x.Score)
                        .ToList();

                var highestOptionScore =
                    rankedOptions.First().Score;

                List<MenuOption> optionsToInspect;

                if (highestOptionScore > 0)
                {
                    optionsToInspect =
                        rankedOptions
                            .Where(x =>
                                x.Score >=
                                highestOptionScore - 2)
                            .Take(5)
                            .Select(x => x.Option)
                            .ToList();
                }
                else
                {
                    /*
                     * Do not guess among a large number
                     * of possible configurations.
                     */
                    if (options.Count > 5)
                    {
                        return vehicle;
                    }

                    optionsToInspect =
                        options;
                }

                var candidates =
                    new List<VehicleCandidate>();

                foreach (var option in optionsToInspect)
                {
                    var candidate =
                        await GetVehicleAsync(
                            option.Id,
                            cancellationToken);

                    if (candidate == null)
                    {
                        continue;
                    }

                    candidate.Score =
                        ScoreCandidate(
                            vehicle,
                            candidate);

                    candidates.Add(
                        candidate);
                }

                if (candidates.Count == 0)
                {
                    return vehicle;
                }

                candidates =
                    candidates
                        .OrderByDescending(
                            x => x.Score)
                        .ToList();

                bool changed;

                if (candidates.Count == 1)
                {
                    changed =
                        ApplyCandidate(
                            vehicle,
                            candidates[0]);
                }
                else
                {
                    var best =
                        candidates[0];

                    var second =
                        candidates[1];

                    if (best.Score >= 8 &&
                        best.Score -
                        second.Score >= 2)
                    {
                        changed =
                            ApplyCandidate(
                                vehicle,
                                best);
                    }
                    else
                    {
                        changed =
                            ApplyConsensus(
                                vehicle,
                                candidates);
                    }
                }

                if (changed)
                {
                    AddSource(
                        vehicle);
                }

                return vehicle;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                /*
                 * Enrichment is optional.
                 * Failure must never break
                 * VIN decoding.
                 */
                return vehicle;
            }
        }

        private async Task<List<MenuOption>> GetOptionsAsync(
            int year,
            string make,
            string model,
            CancellationToken cancellationToken)
        {
            string url =
                $"{BaseUrl}/vehicle/menu/options" +
                $"?year={year}" +
                $"&make={Uri.EscapeDataString(make)}" +
                $"&model={Uri.EscapeDataString(model)}";

            string xml =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            var document =
                XDocument.Parse(xml);

            var results =
                new List<MenuOption>();

            foreach (var element
                in document
                    .Descendants()
                    .Where(x =>
                        x.Name.LocalName ==
                        "menuItem"))
            {
                string text =
                    GetChildValue(
                        element,
                        "text");

                string value =
                    GetChildValue(
                        element,
                        "value");

                if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int id))
                {
                    results.Add(
                        new MenuOption(
                            id,
                            text));
                }
            }

            return results;
        }

        private async Task<VehicleCandidate?> GetVehicleAsync(
            int id,
            CancellationToken cancellationToken)
        {
            string url =
                $"{BaseUrl}/vehicle/{id}";

            string xml =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            var document =
                XDocument.Parse(xml);

            var root =
                document.Root;

            if (root == null)
            {
                return null;
            }

            return new VehicleCandidate
            {
                Id =
                    id,

                Model =
                    GetChildValue(
                        root,
                        "model"),

                Transmission =
                    GetChildValue(
                        root,
                        "trany"),

                Drivetrain =
                    GetChildValue(
                        root,
                        "drive"),

                FuelType =
                    GetChildValue(
                        root,
                        "fuelType"),

                VehicleClass =
                    GetChildValue(
                        root,
                        "VClass"),

                Displacement =
                    ParseDecimal(
                        GetChildValue(
                            root,
                            "displ")),

                Cylinders =
                    ParseInt(
                        GetChildValue(
                            root,
                            "cylinders")),

                CityMpg =
                    ParseInt(
                        GetChildValue(
                            root,
                            "city08")),

                HighwayMpg =
                    ParseInt(
                        GetChildValue(
                            root,
                            "highway08"))
            };
        }

        private static int ScoreOptionText(
            VehicleProfile vehicle,
            string optionText)
        {
            if (string.IsNullOrWhiteSpace(
                optionText))
            {
                return 0;
            }

            int score = 0;

            string text =
                optionText
                    .ToUpperInvariant();

            decimal? displacement =
                GetVehicleDisplacement(
                    vehicle);

            if (displacement.HasValue)
            {
                string displacementText =
                    displacement.Value
                        .ToString(
                            "0.#",
                            CultureInfo.InvariantCulture);

                if (text.Contains(
                    $"{displacementText}L"))
                {
                    score += 10;
                }
            }

            int? cylinders =
                GetVehicleCylinders(
                    vehicle);

            if (cylinders.HasValue)
            {
                if (text.Contains(
                        $"{cylinders.Value}CYL") ||
                    text.Contains(
                        $"{cylinders.Value} CYL"))
                {
                    score += 8;
                }
            }

            string transmission =
                NormalizeTransmission(
                    vehicle.Transmission);

            if (!string.IsNullOrWhiteSpace(
                    transmission) &&
                text.Contains(
                    transmission,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 4;
            }

            string drive =
                NormalizeDrive(
                    vehicle.Drivetrain);

            if (!string.IsNullOrWhiteSpace(
                drive))
            {
                if ((drive == "RWD" &&
                     text.Contains("RWD")) ||

                    (drive == "FWD" &&
                     text.Contains("FWD")) ||

                    (drive == "AWD" &&
                     text.Contains("AWD")) ||

                    (drive == "4WD" &&
                     text.Contains("4WD")))
                {
                    score += 4;
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Trim) &&
                text.Contains(
                    vehicle.Trim,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            return score;
        }

        private static int ScoreCandidate(
            VehicleProfile vehicle,
            VehicleCandidate candidate)
        {
            int score = 0;

            decimal? expectedDisplacement =
                GetVehicleDisplacement(
                    vehicle);

            if (expectedDisplacement.HasValue &&
                candidate.Displacement.HasValue)
            {
                decimal difference =
                    Math.Abs(
                        expectedDisplacement.Value -
                        candidate.Displacement.Value);

                score +=
                    difference <= 0.11m
                        ? 10
                        : -4;
            }

            int? expectedCylinders =
                GetVehicleCylinders(
                    vehicle);

            if (expectedCylinders.HasValue &&
                candidate.Cylinders.HasValue)
            {
                score +=
                    expectedCylinders.Value ==
                    candidate.Cylinders.Value
                        ? 8
                        : -4;
            }

            string expectedFuel =
                NormalizeFuel(
                    vehicle.FuelType);

            string candidateFuel =
                NormalizeFuel(
                    candidate.FuelType);

            if (!string.IsNullOrWhiteSpace(
                    expectedFuel) &&
                !string.IsNullOrWhiteSpace(
                    candidateFuel))
            {
                score +=
                    expectedFuel ==
                    candidateFuel
                        ? 5
                        : -2;
            }

            string expectedTransmission =
                NormalizeTransmission(
                    vehicle.Transmission);

            string candidateTransmission =
                NormalizeTransmission(
                    candidate.Transmission);

            if (!string.IsNullOrWhiteSpace(
                    expectedTransmission) &&
                !string.IsNullOrWhiteSpace(
                    candidateTransmission))
            {
                score +=
                    expectedTransmission ==
                    candidateTransmission
                        ? 4
                        : -1;
            }

            string expectedDrive =
                NormalizeDrive(
                    vehicle.Drivetrain);

            string candidateDrive =
                NormalizeDrive(
                    candidate.Drivetrain);

            if (!string.IsNullOrWhiteSpace(
                    expectedDrive) &&
                !string.IsNullOrWhiteSpace(
                    candidateDrive))
            {
                score +=
                    expectedDrive ==
                    candidateDrive
                        ? 4
                        : -1;
            }

            if (!string.IsNullOrWhiteSpace(
                    vehicle.Trim) &&
                !string.IsNullOrWhiteSpace(
                    candidate.Model) &&
                candidate.Model.Contains(
                    vehicle.Trim,
                    StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }

            return score;
        }

        private static bool ApplyCandidate(
            VehicleProfile vehicle,
            VehicleCandidate candidate)
        {
            bool changed = false;

            if (string.IsNullOrWhiteSpace(
                    vehicle.Transmission) &&
                !string.IsNullOrWhiteSpace(
                    candidate.Transmission))
            {
                vehicle.Transmission =
                    candidate.Transmission;

                vehicle.TransmissionStyle =
                    candidate.Transmission;

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.Drivetrain) &&
                !string.IsNullOrWhiteSpace(
                    candidate.Drivetrain))
            {
                vehicle.Drivetrain =
                    candidate.Drivetrain;

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.FuelType) &&
                !string.IsNullOrWhiteSpace(
                    candidate.FuelType))
            {
                vehicle.FuelType =
                    NormalizeFuelDisplay(
                        candidate.FuelType);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.EngineDisplacement) &&
                candidate.Displacement.HasValue)
            {
                vehicle.EngineDisplacement =
                    candidate.Displacement.Value
                        .ToString(
                            "0.#",
                            CultureInfo.InvariantCulture);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.EngineCylinders) &&
                candidate.Cylinders.HasValue)
            {
                vehicle.EngineCylinders =
                    candidate.Cylinders.Value
                        .ToString(
                            CultureInfo.InvariantCulture);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.Engine))
            {
                string engine =
                    BuildEngineDescription(
                        candidate);

                if (!string.IsNullOrWhiteSpace(
                    engine))
                {
                    vehicle.Engine =
                        engine;

                    changed = true;
                }
            }

            if (!vehicle.CityMpg.HasValue &&
                candidate.CityMpg.HasValue)
            {
                vehicle.CityMpg =
                    candidate.CityMpg.Value;

                changed = true;
            }

            if (!vehicle.HighwayMpg.HasValue &&
                candidate.HighwayMpg.HasValue)
            {
                vehicle.HighwayMpg =
                    candidate.HighwayMpg.Value;

                changed = true;
            }

            return changed;
        }

        private static bool ApplyConsensus(
            VehicleProfile vehicle,
            List<VehicleCandidate> candidates)
        {
            bool changed = false;

            string transmission =
                GetConsensus(
                    candidates.Select(
                        x => x.Transmission));

            string drivetrain =
                GetConsensus(
                    candidates.Select(
                        x => x.Drivetrain));

            string fuelType =
                GetConsensus(
                    candidates.Select(
                        x => x.FuelType));

            decimal? displacement =
                GetConsensusDecimal(
                    candidates.Select(
                        x => x.Displacement));

            int? cylinders =
                GetConsensusInt(
                    candidates.Select(
                        x => x.Cylinders));

            int? cityMpg =
                GetConsensusInt(
                    candidates.Select(
                        x => x.CityMpg));

            int? highwayMpg =
                GetConsensusInt(
                    candidates.Select(
                        x => x.HighwayMpg));

            if (string.IsNullOrWhiteSpace(
                    vehicle.Transmission) &&
                !string.IsNullOrWhiteSpace(
                    transmission))
            {
                vehicle.Transmission =
                    transmission;

                vehicle.TransmissionStyle =
                    transmission;

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.Drivetrain) &&
                !string.IsNullOrWhiteSpace(
                    drivetrain))
            {
                vehicle.Drivetrain =
                    drivetrain;

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.FuelType) &&
                !string.IsNullOrWhiteSpace(
                    fuelType))
            {
                vehicle.FuelType =
                    NormalizeFuelDisplay(
                        fuelType);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.EngineDisplacement) &&
                displacement.HasValue)
            {
                vehicle.EngineDisplacement =
                    displacement.Value
                        .ToString(
                            "0.#",
                            CultureInfo.InvariantCulture);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.EngineCylinders) &&
                cylinders.HasValue)
            {
                vehicle.EngineCylinders =
                    cylinders.Value
                        .ToString(
                            CultureInfo.InvariantCulture);

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(
                    vehicle.Engine) &&
                displacement.HasValue &&
                cylinders.HasValue)
            {
                vehicle.Engine =
                    $"{displacement.Value:0.#}L " +
                    $"{cylinders.Value} Cyl";

                changed = true;
            }

            if (!vehicle.CityMpg.HasValue &&
                cityMpg.HasValue)
            {
                vehicle.CityMpg =
                    cityMpg.Value;

                changed = true;
            }

            if (!vehicle.HighwayMpg.HasValue &&
                highwayMpg.HasValue)
            {
                vehicle.HighwayMpg =
                    highwayMpg.Value;

                changed = true;
            }

            return changed;
        }

        private static decimal? GetVehicleDisplacement(
            VehicleProfile vehicle)
        {
            if (decimal.TryParse(
                vehicle.EngineDisplacement,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal displacement))
            {
                return displacement;
            }

            Match match =
                Regex.Match(
                    vehicle.Engine ??
                    string.Empty,
                    @"(?<value>\d+(?:\.\d+)?)\s*L",
                    RegexOptions.IgnoreCase);

            if (match.Success &&
                decimal.TryParse(
                    match.Groups["value"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out displacement))
            {
                return displacement;
            }

            return null;
        }

        private static int? GetVehicleCylinders(
            VehicleProfile vehicle)
        {
            if (int.TryParse(
                vehicle.EngineCylinders,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int cylinders))
            {
                return cylinders;
            }

            Match match =
                Regex.Match(
                    vehicle.Engine ??
                    string.Empty,
                    @"(?<value>\d+)\s*(?:CYL|CYLINDER)",
                    RegexOptions.IgnoreCase);

            if (match.Success &&
                int.TryParse(
                    match.Groups["value"].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out cylinders))
            {
                return cylinders;
            }

            return null;
        }

        private static string NormalizeTransmission(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return string.Empty;
            }

            string text =
                value
                    .ToUpperInvariant();

            if (text.Contains("CVT"))
            {
                return "CVT";
            }

            if (text.Contains("AUTO"))
            {
                return "AUTOMATIC";
            }

            if (text.Contains("MANUAL"))
            {
                return "MANUAL";
            }

            return string.Empty;
        }

        private static string NormalizeDrive(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return string.Empty;
            }

            string text =
                value
                    .ToUpperInvariant();

            if (text.Contains("REAR") ||
                text.Contains("RWD"))
            {
                return "RWD";
            }

            if (text.Contains("FRONT") ||
                text.Contains("FWD"))
            {
                return "FWD";
            }

            if (text.Contains("ALL-WHEEL") ||
                text.Contains("ALL WHEEL") ||
                text.Contains("AWD"))
            {
                return "AWD";
            }

            if (text.Contains("4-WHEEL") ||
                text.Contains("4 WHEEL") ||
                text.Contains("4WD") ||
                text.Contains("4X4"))
            {
                return "4WD";
            }

            return string.Empty;
        }

        private static string NormalizeFuel(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return string.Empty;
            }

            string text =
                value
                    .ToUpperInvariant();

            if (text.Contains("DIESEL"))
            {
                return "DIESEL";
            }

            if (text.Contains("E85") ||
                text.Contains("FLEX"))
            {
                return "E85";
            }

            if (text.Contains("ELECTRIC"))
            {
                return "ELECTRIC";
            }

            if (text.Contains("HYBRID"))
            {
                return "HYBRID";
            }

            if (text.Contains("CNG"))
            {
                return "CNG";
            }

            if (text.Contains("PROPANE") ||
                text.Contains("LPG"))
            {
                return "LPG";
            }

            if (text.Contains("HYDROGEN"))
            {
                return "HYDROGEN";
            }

            if (text.Contains("GASOLINE") ||
                text == "GAS")
            {
                return "GASOLINE";
            }

            return text.Trim();
        }

        private static string NormalizeFuelDisplay(
            string value)
        {
            return NormalizeFuel(value) switch
            {
                "GASOLINE" => "Gasoline",
                "DIESEL" => "Diesel",
                "E85" => "E85",
                "ELECTRIC" => "Electric",
                "HYBRID" => "Hybrid",
                "CNG" => "CNG",
                "LPG" => "LPG",
                "HYDROGEN" => "Hydrogen",
                _ => value.Trim()
            };
        }

        private static string BuildEngineDescription(
            VehicleCandidate candidate)
        {
            var parts =
                new List<string>();

            if (candidate.Displacement.HasValue)
            {
                parts.Add(
                    $"{candidate.Displacement.Value:0.#}L");
            }

            if (candidate.Cylinders.HasValue)
            {
                parts.Add(
                    $"{candidate.Cylinders.Value} Cyl");
            }

            return string.Join(
                " ",
                parts);
        }

        private static string GetConsensus(
            IEnumerable<string?> values)
        {
            var distinct =
                values
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Select(x =>
                        x!.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return distinct.Count == 1
                ? distinct[0]
                : string.Empty;
        }

        private static decimal? GetConsensusDecimal(
            IEnumerable<decimal?> values)
        {
            var distinct =
                values
                    .Where(x =>
                        x.HasValue)
                    .Select(x =>
                        x!.Value)
                    .Distinct()
                    .ToList();

            return distinct.Count == 1
                ? distinct[0]
                : null;
        }

        private static int? GetConsensusInt(
            IEnumerable<int?> values)
        {
            var distinct =
                values
                    .Where(x =>
                        x.HasValue)
                    .Select(x =>
                        x!.Value)
                    .Distinct()
                    .ToList();

            return distinct.Count == 1
                ? distinct[0]
                : null;
        }

        private static string GetChildValue(
            XElement element,
            string name)
        {
            return element
                .Elements()
                .FirstOrDefault(x =>
                    x.Name.LocalName.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase))
                ?.Value
                ?.Trim()
                ?? string.Empty;
        }

        private static decimal? ParseDecimal(
            string value)
        {
            return decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal result)
                    ? result
                    : null;
        }

        private static int? ParseInt(
            string value)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result)
                    ? result
                    : null;
        }

        private static void AddSource(
            VehicleProfile vehicle)
        {
            const string source =
                "FuelEconomy.gov";

            if (string.IsNullOrWhiteSpace(
                    vehicle.DecodeSource))
            {
                vehicle.DecodeSource =
                    source;

                return;
            }

            if (!vehicle.DecodeSource.Contains(
                    source,
                    StringComparison.OrdinalIgnoreCase))
            {
                vehicle.DecodeSource +=
                    $" + {source}";
            }
        }

        private sealed record MenuOption(
            int Id,
            string Text);

        private sealed class VehicleCandidate
        {
            public int Id { get; init; }

            public string Model { get; init; } =
                string.Empty;

            public string Transmission { get; init; } =
                string.Empty;

            public string Drivetrain { get; init; } =
                string.Empty;

            public string FuelType { get; init; } =
                string.Empty;

            public string VehicleClass { get; init; } =
                string.Empty;

            public decimal? Displacement { get; init; }

            public int? Cylinders { get; init; }

            public int? CityMpg { get; init; }

            public int? HighwayMpg { get; init; }

            public int Score { get; set; }
        }
    }
}
