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

        public BuyAnalyzerModel(
            IBuyScoringService buyScoringService,
            IVinDecoderService vinDecoderService)
        {
            _buyScoringService = buyScoringService;
            _vinDecoderService = vinDecoderService;
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

                var warning =
                    decoded.DecodeWarnings.FirstOrDefault()
                    ?? "The VIN could not be decoded.";

                ModelState.AddModelError(
                    "Input.Vin",
                    warning);

                return Page();
            }

            /*
             * VIN-decoded information takes priority when
             * NHTSA actually returned a value.
             *
             * Empty NHTSA values do NOT erase information
             * already entered by the user.
             */

            Input.Vin = decoded.Vin;

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

            /*
             * Clear the validation state created from the
             * pre-decode form so Razor displays the newly
             * decoded values instead.
             */
            ModelState.Clear();

            VinDecodeMessage =
                $"VIN decoded: {decoded.DisplayName}";

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Result = _buyScoringService.Analyze(Input);

            return Page();
        }
    }
}
