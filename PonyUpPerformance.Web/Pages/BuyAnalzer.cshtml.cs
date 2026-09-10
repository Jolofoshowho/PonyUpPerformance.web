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

        public BuyAnalyzerModel(
            IBuyScoringService buyScoringService,
            IVinDecoderService vinDecoderService,
            IVehicleSpecEnrichmentService vehicleSpecEnrichmentService)
        {
            _buyScoringService = buyScoringService;
            _vinDecoderService = vinDecoderService;
            _vehicleSpecEnrichmentService = vehicleSpecEnrichmentService;
        }

        [BindProperty]
        public BuyDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; private set; }

        public string VinDecodeMessage { get; private set; } = string.Empty;

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

            decoded =
                await _vehicleSpecEnrichmentService.EnrichAsync(
                    decoded,
                    cancellationToken);

            ApplyDecodedVehicle(decoded);

            ModelState.Clear();

            VinDecodeMessage =
                string.IsNullOrWhiteSpace(decoded.DisplayName)
                    ? "VIN decoded successfully."
                    : $"VIN decoded: {decoded.DisplayName}";

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Result =
                _buyScoringService.Analyze(Input);

            return Page();
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

            if (!string.IsNullOrWhiteSpace(decoded.Transmission))
            {
                Input.Transmission = decoded.Transmission;
            }

            if (!string.IsNullOrWhiteSpace(decoded.Drivetrain))
            {
                Input.Drivetrain = decoded.Drivetrain;
            }

            if (!string.IsNullOrWhiteSpace(decoded.BodyStyle))
            {
                Input.BodyStyle = decoded.BodyStyle;
            }

            if (!string.IsNullOrWhiteSpace(decoded.FuelType))
            {
                Input.FuelType = decoded.FuelType;
            }

            if (decoded.BaseMsrp.HasValue &&
                decoded.BaseMsrp.Value > 0)
            {
                Input.BaseMsrp = decoded.BaseMsrp.Value;
            }
        }
    }
}
