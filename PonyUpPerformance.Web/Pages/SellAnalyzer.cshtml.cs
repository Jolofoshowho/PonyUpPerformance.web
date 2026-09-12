using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class SellAnalyzerModel : PageModel
{
    private readonly ISellScoringService _sellScoringService;
    private readonly IVinDecoderService _vinDecoderService;
    private readonly IVehicleSpecEnrichmentService _vehicleSpecEnrichmentService;
    private readonly IMarketValueService _marketValueService;

    public SellAnalyzerModel(
        ISellScoringService sellScoringService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService,
        IMarketValueService marketValueService)
    {
        _sellScoringService =
            sellScoringService;

        _vinDecoderService =
            vinDecoderService;

        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;

        _marketValueService =
            marketValueService;
    }

    [BindProperty]
    public SellDecisionInput Input { get; set; } = new();

    public SellDecisionResult? Result { get; private set; }

    public string VinDecodeMessage { get; private set; }
        = string.Empty;

    public string MarketValueMessage { get; private set; }
        = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostDecodeVinAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
            Input.Vin))
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
                decoded.DecodeWarnings
                    .FirstOrDefault()
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

        /*
         * Mileage is not VIN decoded.
         * If the user supplied it, pass it into
         * the live market-value request.
         */
        if (Input.Mileage.HasValue)
        {
            decoded.CurrentMileage =
                Input.Mileage.Value;
        }

        /*
         * These fields are currently manual unless
         * a future vehicle-history provider populates
         * VehicleProfile before this point.
         */
        if (Input.TitleStatus !=
            TitleStatus.NotProvided)
        {
            decoded.TitleStatus =
                Input.TitleStatus;
        }

        if (Input.AccidentHistory !=
            AccidentHistory.NotProvided)
        {
            decoded.AccidentHistory =
                Input.AccidentHistory;
        }

        ApplyDecodedVehicle(decoded);

        MarketValueResult valuation =
            await _marketValueService.AnalyzeAsync(
                decoded,
                cancellationToken);

        if (valuation.HasEstimate)
        {
            /*
             * Preserve an explicit user-entered value.
             * Otherwise PonyUp fills the field from
             * the live valuation result.
             */
            if (!Input.MarketValue.HasValue)
            {
                Input.MarketValue =
                    valuation.EstimatedMarketValue;
            }

            MarketValueMessage =
                $"PonyUp live market value estimate: " +
                $"{valuation.EstimatedMarketValue:C0}.";
        }
        else
        {
            MarketValueMessage =
                valuation.Summary;
        }

        ModelState.Clear();

        VinDecodeMessage =
            string.IsNullOrWhiteSpace(
                decoded.DisplayName)
                ? "VIN decoded successfully."
                : $"VIN decoded: {decoded.DisplayName}";

        return Page();
    }

    public IActionResult OnPostAnalyze()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result =
            _sellScoringService.Analyze(
                Input);

        return Page();
    }

    private void ApplyDecodedVehicle(
        VehicleProfile decoded)
    {
        if (!string.IsNullOrWhiteSpace(
            decoded.Vin))
        {
            Input.Vin =
                decoded.Vin;
        }

        if (decoded.Year.HasValue)
        {
            Input.Year =
                decoded.Year.Value;
        }

        if (!string.IsNullOrWhiteSpace(
            decoded.Make))
        {
            Input.Make =
                decoded.Make;
        }

        if (!string.IsNullOrWhiteSpace(
            decoded.Model))
        {
            Input.Model =
                decoded.Model;
        }

        if (!string.IsNullOrWhiteSpace(
            decoded.Trim))
        {
            Input.Trim =
                decoded.Trim;
        }

        /*
         * NHTSA VIN decoding does NOT currently
         * provide these values.
         *
         * This mapping is intentionally present so
         * a future authoritative history provider
         * can enrich VehicleProfile without requiring
         * another Sell Analyzer rewrite.
         */
        if (decoded.TitleStatus !=
            TitleStatus.NotProvided)
        {
            Input.TitleStatus =
                decoded.TitleStatus;
        }

        if (decoded.AccidentHistory !=
            AccidentHistory.NotProvided)
        {
            Input.AccidentHistory =
                decoded.AccidentHistory;
        }
    }
}
