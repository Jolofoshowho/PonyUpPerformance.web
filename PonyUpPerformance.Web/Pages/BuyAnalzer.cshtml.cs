using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages
{
    public class BuyAnalyzerModel : PageModel
    {
        private readonly IBuyScoringService _buyScoringService;
        private readonly IVinDecoderService _vinDecoderService;
        private readonly IVehicleSpecEnrichmentService _vehicleSpecEnrichmentService;
        private readonly IMarketValueService _marketValueService;

        public BuyAnalyzerModel(
            IBuyScoringService buyScoringService,
            IVinDecoderService vinDecoderService,
            IVehicleSpecEnrichmentService vehicleSpecEnrichmentService,
            IMarketValueService marketValueService)
        {
            _buyScoringService = buyScoringService;
            _vinDecoderService = vinDecoderService;
            _vehicleSpecEnrichmentService =
                vehicleSpecEnrichmentService;
            _marketValueService = marketValueService;
        }

        [BindProperty]
        public BuyDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; private set; }

        public MarketValueResult? MarketValueSuggestion
        {
            get;
            private set;
        }

        public string VinDecodeMessage
        {
            get;
            private set;
        } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostDecodeVinAsync(
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Input.Vin))
            {
                ModelState.Clear();

                ModelState.AddModelError(
                    "Input.Vin",
                    "Enter a VIN to decode.");

                return Page();
            }

            VehicleProfile decoded =
                await _vinDecoderService.DecodeAsync(
                    Input.Vin,
                    cancellationToken);

            if (!decoded.DecodeSuccessful)
            {
                ModelState.Clear();

                string warning =
                    decoded.DecodeWarnings.FirstOrDefault()
                    ?? "The VIN could not be decoded.";

                ModelState.AddModelError(
                    "Input.Vin",
                    warning);

                return Page();
            }

            /*
             * NHTSA handles the primary VIN decode.
             * FuelEconomy.gov then fills missing factory
             * specifications without replacing valid
             * decoded values.
             */
            decoded =
                await _vehicleSpecEnrichmentService.EnrichAsync(
                    decoded,
                    cancellationToken);

            ApplyDecodedVehicle(decoded);

            /*
             * Request a market-value suggestion after
             * vehicle identity/specification enrichment.
             */
            await ApplyMarketSuggestionsAsync(
                cancellationToken);

            /*
             * Clear the submitted binding state so Razor
             * displays the decoded and enriched values.
             */
            ModelState.Clear();

            VinDecodeMessage =
                string.IsNullOrWhiteSpace(decoded.DisplayName)
                    ? "VIN decoded successfully."
                    : $"VIN decoded: {decoded.DisplayName}";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            /*
             * Request market value only when one of the
             * relevant fields still needs a suggestion.
             *
             * If VIN decoding already populated both the
             * market value and asking price, this method
             * will not consume another Vehicles.dev call.
             */
            await ApplyMarketSuggestionsAsync(
                cancellationToken);

            Result =
                _buyScoringService.Analyze(Input);

            return Page();
        }

        private async Task ApplyMarketSuggestionsAsync(
            CancellationToken cancellationToken)
        {
            /*
             * Protect the Vehicles.dev free-call allowance.
             *
             * If both values are already present, there is
             * nothing left for the market service to supply.
             */
            if (Input.MarketValue.HasValue &&
                Input.AskingPrice.HasValue)
            {
                return;
            }

            VehicleProfile profile =
                BuildVehicleProfileFromInput();

            MarketValueSuggestion =
                await _marketValueService.AnalyzeAsync(
                    profile,
                    cancellationToken);

            if (!MarketValueSuggestion.HasEstimate)
            {
                return;
            }

            /*
             * Never overwrite a value the user already
             * supplied.
             */
            if (!Input.MarketValue.HasValue)
            {
                Input.MarketValue =
                    MarketValueSuggestion
                        .EstimatedMarketValue;
            }

            if (!Input.AskingPrice.HasValue)
            {
                Input.AskingPrice =
                    MarketValueSuggestion
                        .SuggestedAskingPrice;
            }
        }

        private VehicleProfile BuildVehicleProfileFromInput()
        {
            return new VehicleProfile
            {
                Vin =
                    Input.Vin
                    ?? string.Empty,

                Year =
                    Input.Year,

                Make =
                    Input.Make
                    ?? string.Empty,

                Model =
                    Input.Model
                    ?? string.Empty,

                Trim =
                    Input.Trim
                    ?? string.Empty,

                Engine =
                    Input.Engine
                    ?? string.Empty,

                Transmission =
                    Input.Transmission
                    ?? string.Empty,

                TransmissionStyle =
                    Input.Transmission
                    ?? string.Empty,

                Drivetrain =
                    Input.Drivetrain
                    ?? string.Empty,

                BodyStyle =
                    Input.BodyStyle
                    ?? string.Empty,

                FuelType =
                    Input.FuelType
                    ?? string.Empty,

                BaseMsrp =
                    Input.BaseMsrp,

                CurrentMileage =
                    Input.Mileage,

                MechanicalCondition =
                    Input.MechanicalCondition,

                TitleStatus =
                    Input.TitleStatus,

                AccidentHistory =
                    Input.AccidentHistory,

                DecodeSuccessful =
                    Input.Year.HasValue &&
                    !string.IsNullOrWhiteSpace(
                        Input.Make) &&
                    !string.IsNullOrWhiteSpace(
                        Input.Model)
            };
        }

        private void ApplyDecodedVehicle(
            VehicleProfile decoded)
        {
            if (!string.IsNullOrWhiteSpace(decoded.Vin))
            {
                Input.Vin = decoded.Vin;
            }

            if (decoded.Year.HasValue)
            {
                Input.Year = decoded.Year.Value;
            }

            if (!string.IsNullOrWhiteSpace(decoded.Make))
            {
                Input.Make = decoded.Make;
            }

            if (!string.IsNullOrWhiteSpace(decoded.Model))
            {
                Input.Model = decoded.Model;
            }

            if (!string.IsNullOrWhiteSpace(decoded.Trim))
            {
                Input.Trim = decoded.Trim;
            }

            if (!string.IsNullOrWhiteSpace(decoded.Engine))
            {
                Input.Engine = decoded.Engine;
            }

            if (!string.IsNullOrWhiteSpace(
                    decoded.Transmission))
            {
                Input.Transmission =
                    decoded.Transmission;
            }

            if (!string.IsNullOrWhiteSpace(
                    decoded.Drivetrain))
            {
                Input.Drivetrain =
                    decoded.Drivetrain;
            }

            if (!string.IsNullOrWhiteSpace(
                    decoded.BodyStyle))
            {
                Input.BodyStyle =
                    decoded.BodyStyle;
            }

            if (!string.IsNullOrWhiteSpace(
                    decoded.FuelType))
            {
                Input.FuelType =
                    decoded.FuelType;
            }

            if (decoded.BaseMsrp.HasValue &&
                decoded.BaseMsrp.Value > 0)
            {
                Input.BaseMsrp =
                    decoded.BaseMsrp.Value;
            }
        }
    }
}
