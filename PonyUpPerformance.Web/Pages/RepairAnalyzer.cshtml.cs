using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class RepairAnalyzerModel : PageModel
{
    private readonly IRepairScoringService _repairScoringService;
    private readonly RepairCostEstimatorService _repairCostEstimatorService;
    private readonly UsageCreditService _usageCreditService;
    private readonly IVinDecoderService _vinDecoderService;
    private readonly IVehicleSpecEnrichmentService _vehicleSpecEnrichmentService;

    public RepairAnalyzerModel(
        IRepairScoringService repairScoringService,
        RepairCostEstimatorService repairCostEstimatorService,
        UsageCreditService usageCreditService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService)
    {
        _repairScoringService = repairScoringService;
        _repairCostEstimatorService = repairCostEstimatorService;
        _usageCreditService = usageCreditService;
        _vinDecoderService = vinDecoderService;
        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;
    }

    [BindProperty]
    public RepairDecisionInput Input { get; set; } = new();

    [BindProperty]
    public RepairCostEstimateInput EstimateInput { get; set; } = new();

    [BindProperty]
    public bool EstimateCreditConsumed { get; set; }

    public RepairCostEstimateResult? EstimateResult { get; set; }

    public DecisionResult? Result { get; set; }

    public UsageCreditStatus CreditStatus { get; set; } = new();

    public string? CreditMessage { get; set; }

    public string VinDecodeMessage { get; private set; } =
        string.Empty;

    public List<string> RepairTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        RepairTypes =
            _repairCostEstimatorService.GetRepairTypes();

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);
    }

    public async Task<IActionResult> OnPostDecodeVinAsync(
        CancellationToken cancellationToken)
    {
        RepairTypes =
            _repairCostEstimatorService.GetRepairTypes();

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

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

        /*
         * Clear posted binding values so Razor displays
         * the newly decoded vehicle information.
         */
        ModelState.Clear();

        VinDecodeMessage =
            string.IsNullOrWhiteSpace(decoded.DisplayName)
                ? "VIN decoded successfully."
                : $"VIN decoded: {decoded.DisplayName}";

        return Page();
    }

    public async Task<IActionResult> OnPostEstimateAsync()
    {
        RepairTypes =
            _repairCostEstimatorService.GetRepairTypes();

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage =
                "Create a free account to estimate the repair cost.";

            return Page();
        }

        if (!CreditStatus.CanRunAnalysis)
        {
            CreditMessage =
                "You are out of analysis credits. Choose a plan to continue.";

            return Page();
        }

        EstimateInput.VehicleYear =
            Input.VehicleYear;

        EstimateInput.VehicleMake =
            Input.VehicleMake;

        EstimateInput.VehicleModel =
            Input.VehicleModel;

        EstimateResult =
            _repairCostEstimatorService.Estimate(
                EstimateInput);

        bool consumed =
            await _usageCreditService.ConsumeCreditAsync(
                User,
                "Repair Cost Estimate");

        if (!consumed)
        {
            CreditMessage =
                "Unable to consume an analysis credit.";

            CreditStatus =
                await _usageCreditService.GetStatusAsync(User);

            EstimateResult = null;

            return Page();
        }

        Input.RepairCost =
            EstimateResult.ExpectedEstimate;

        /*
         * Force Razor to display the estimated repair
         * value instead of the stale posted value.
         */
        ModelState.Remove(
            nameof(Input.RepairCost));

        EstimateCreditConsumed = true;

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        return Page();
    }

    public async Task<IActionResult> OnPostAnalyzeAsync()
    {
        RepairTypes =
            _repairCostEstimatorService.GetRepairTypes();

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage =
                "Create a free account to run a repair analysis.";

            return Page();
        }

        if (Input.RepairCost <= 0)
        {
            CreditMessage =
                "Estimate the repair cost first or enter a valid repair cost.";

            return Page();
        }

        if (!EstimateCreditConsumed)
        {
            if (!CreditStatus.CanRunAnalysis)
            {
                CreditMessage =
                    "You are out of analysis credits.";

                return Page();
            }

            bool consumed =
                await _usageCreditService.ConsumeCreditAsync(
                    User,
                    "Repair");

            if (!consumed)
            {
                CreditMessage =
                    "Unable to consume an analysis credit.";

                CreditStatus =
                    await _usageCreditService.GetStatusAsync(User);

                return Page();
            }
        }

        Result =
            _repairScoringService.Analyze(Input);

        EstimateCreditConsumed = false;

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        return Page();
    }

    private void ApplyDecodedVehicle(
        VehicleProfile decoded)
    {
        if (!string.IsNullOrWhiteSpace(decoded.Vin))
        {
            Input.Vin =
                decoded.Vin;
        }

        if (decoded.Year.HasValue)
        {
            Input.VehicleYear =
                decoded.Year.Value;
        }

        if (!string.IsNullOrWhiteSpace(decoded.Make))
        {
            Input.VehicleMake =
                decoded.Make;
        }

        if (!string.IsNullOrWhiteSpace(decoded.Model))
        {
            Input.VehicleModel =
                decoded.Model;
        }
    }
}
