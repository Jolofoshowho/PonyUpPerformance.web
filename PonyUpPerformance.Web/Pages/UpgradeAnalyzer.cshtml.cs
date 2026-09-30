using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class UpgradeAnalyzerModel : PageModel
{
    private readonly IUpgradeScoringService
        _upgradeScoringService;

    private readonly IVinDecoderService
        _vinDecoderService;

    private readonly IVehicleSpecEnrichmentService
        _vehicleSpecEnrichmentService;

    private readonly AnalysisHistoryService
        _analysisHistoryService;

    private readonly UsageCreditService
        _usageCreditService;

    public UpgradeAnalyzerModel(
        IUpgradeScoringService upgradeScoringService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService,
        AnalysisHistoryService analysisHistoryService,
        UsageCreditService usageCreditService)
    {
        _upgradeScoringService =
            upgradeScoringService;

        _vinDecoderService =
            vinDecoderService;

        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;

        _analysisHistoryService =
            analysisHistoryService;

        _usageCreditService =
            usageCreditService;
    }

    [BindProperty]
    public UpgradeDecisionInput Input { get; set; } =
        new();

    public UpgradeDecisionResult? Result { get; private set; }

    public string VinDecodeMessage { get; private set; } =
        string.Empty;

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

        ApplyDecodedVehicle(
            decoded);

        ModelState.Clear();

        VinDecodeMessage =
            string.IsNullOrWhiteSpace(
                decoded.DisplayName)
                ? "VIN decoded successfully."
                : $"VIN decoded: {decoded.DisplayName}";

        return Page();
    }

    public async Task<IActionResult> OnPostAnalyzeAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        UsageCreditStatus creditStatus =
            await _usageCreditService.GetStatusAsync(
                User);

        if (!creditStatus.IsLoggedIn)
        {
            ModelState.AddModelError(
                string.Empty,
                "Create a free account to run a upgrade analysis.");

            return Page();
        }

        if (!creditStatus.CanRunAnalysis)
        {
            ModelState.AddModelError(
                string.Empty,
                "You are out of analysis credits. Upgrade to continue.");

            return Page();
        }

        bool consumed =
            await _usageCreditService.ConsumeCreditAsync(
                User,
                "Upgrade");

        if (!consumed)
        {
            ModelState.AddModelError(
                string.Empty,
                "Unable to consume an analysis credit.");

            return Page();
        }

        Result =
            _upgradeScoringService.Analyze(
                Input);

        await _analysisHistoryService.SaveAnalysisAsync(
            User,
            "Upgrade",
            Input.Year,
            Input.Make,
            Input.Model,
            null,
            Input.CurrentValue,
            Result,
            expectedEstimate:
                Input.UpgradeCost);

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

        if (decoded.Horsepower.HasValue &&
            decoded.Horsepower.Value > 0)
        {
            Input.CurrentHorsepower =
                decoded.Horsepower.Value;
        }
    }
}
