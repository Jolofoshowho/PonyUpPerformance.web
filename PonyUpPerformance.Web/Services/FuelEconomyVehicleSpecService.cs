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

                            Score =
                                ScoreOptionText(
                                    vehicle,
                                    option.Text)
                        })
                        .OrderByDescending(
                            x => x.Score)
                        .ToList();

                var highestOptionScore =
                    rankedOptions
                        .First()
                        .Score;

                List<MenuOption> optionsToInspect;

                if (highestOptionScore > 0)
                {
                    optionsToInspect =
                        rankedOptions
                            .Where(
                                x =>
                                    x.Score >=
                                    highestOptionScore - 2)
                            .Take(5)
                            .Select(
                                x => x.Option)
                            .ToList();
                }
                else
                {
                    /*
                     * With no identifying evidence,
                     * do not arbitrarily choose among
                     * many configurations.
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

                foreach (
                    var option
                    in optionsToInspect)
                {
                    var candidate =
                        await GetVehicleAsync(
                            option.Id,
                            cancellationToken);

                    if (candidate != null)
                    {
                        candidate.Score =
                            ScoreCandidate(
                                vehicle,
                                candidate);

                        candidates.Add(
                            candidate);
                    }
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

                    /*
                     * A strong, clearly better
                     * configuration may be selected
                     * directly.
                     *
                     * Otherwise only values shared
                     * by all viable candidates are
                     * applied.
                     */
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
            var url =
                $"{BaseUrl}/vehicle/menu/options" +
                $"?year={year}" +
                $"&make={Uri.EscapeDataString(make)}" +
                $"&model={Uri.EscapeDataString(model)}";

            var xml =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            var document =
                XDocument.Parse(xml);

            var results =
                new List<MenuOption>();

            foreach (
                var element
                in document
                    .Descendants()
                    .Where(
                        x =>
                            x.Name.LocalName ==
                            "menuItem"))
            {
                var text =
                    GetChildValue(
                        element,
                        "text");

                var value =
                    GetChildValue(
                        element,
                        "value");

                if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var id))
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
            var url =
                $"{BaseUrl}/vehicle/{id}";

            var xml =
                await _httpClient.GetStringAsync(
                    url,
                    cancellationToken);

            var document =
                XDocument.Parse(xml);

            var root =
                document.Root;
                }
