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

    public SellAnalyzerModel(
        ISellScoringService sellScoringService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService)
    {
        _sellScoringService =
            sellScoringService;

        _vinDecoderService =
            vinDecoderService;

        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;
    }

    [BindProperty]
    public SellDecisionInput Input { get; set; } = new();

    public SellDecisionResult? Result { get; private set; }

    public string VinDecodeMessage { get; private set; }
        = string.Empty;

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

    public IActionResult OnPostAnalyze()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result =
            _sellScoringService.Analyze(Input);

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
    }
}
