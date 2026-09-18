using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class RepairAnalyzerModel : PageModel
{
    private readonly IRepairScoringService
        _repairScoringService;

    private readonly RepairCostEstimatorService
        _repairCostEstimatorService;

    private readonly UsageCreditService
        _usageCreditService;

    private readonly IVinDecoderService
        _vinDecoderService;

    private readonly IVehicleSpecEnrichmentService
        _vehicleSpecEnrichmentService;

    public RepairAnalyzerModel(
        IRepairScoringService repairScoringService,
        RepairCostEstimatorService repairCostEstimatorService,
        UsageCreditService usageCreditService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService)
    {
        _repairScoringService =
            repairScoringService;

        _repairCostEstimatorService =
            repairCostEstimatorService;

        _usageCreditService =
            usageCreditService;

        _vinDecoderService =
            vinDecoderService;

        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;
    }

    [BindProperty]
    public RepairDecisionInput Input { get; set; } =
        new();

    [BindProperty]
    public RepairCostEstimateInput EstimateInput { get; set; } =
        new();

    [BindProperty]
    public bool EstimateCreditConsumed { get; set; }

    [BindProperty]
    public int WorkflowStage { get; set; } = 1;

    [BindProperty]
    public RepairCostEstimateResult? EstimateResult { get; set; }

    public DecisionResult? Result { get; set; }

    public UsageCreditStatus CreditStatus { get; set; } =
        new();

    public string? CreditMessage { get; set; }

    public string VinDecodeMessage { get; private set; } =
        string.Empty;

    public List<string> RepairTypes { get; set; } =
        new();

    public async Task OnGetAsync()
    {
        WorkflowStage = 1;

        await LoadPageStateAsync();
    }

    public async Task<IActionResult> OnPostDecodeVinAsync(
        CancellationToken cancellationToken)
    {
        WorkflowStage = 1;

        await LoadPageStateAsync();

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

        WorkflowStage = 1;

        return Page();
    }

    public async Task<IActionResult>
        OnPostEstimateAsync()
    {
        WorkflowStage = 1;

        await LoadPageStateAsync();

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
            Input.VehicleYear ?? 0;

        EstimateInput.VehicleMake =
            Input.VehicleMake
            ?? string.Empty;

        EstimateInput.VehicleModel =
            Input.VehicleModel
            ?? string.Empty;

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
                await _usageCreditService.GetStatusAsync(
                    User);

            EstimateResult = null;

            WorkflowStage = 1;

            return Page();
        }

        Input.RepairCost =
            EstimateResult.ExpectedEstimate;

        ModelState.Remove(
            "Input.RepairCost");

        EstimateCreditConsumed = true;

        CreditStatus =
            await _usageCreditService.GetStatusAsync(
                User);

        WorkflowStage = 2;

        return Page();
    }

    /*
     * Repair estimation is optional.
     *
     * A customer may already have a written quote,
     * may want to enter the cost manually, or may
     * simply want PonyUp to evaluate the evidence
     * they currently have.
     */
    public async Task<IActionResult>
        OnPostContinueToAnalysisAsync()
    {
        WorkflowStage = 2;

        EstimateResult = null;

        await LoadPageStateAsync();

        ModelState.Clear();

        return Page();
    }

    public async Task<IActionResult>
        OnPostAnalyzeAsync()
    {
        WorkflowStage = 2;

        await LoadPageStateAsync();

        /*
         * Nothing is mandatory.
         *
         * Blank nullable fields are valid.
         * Truly invalid supplied values still need
         * to be corrected.
         */
        if (!ModelState.IsValid)
        {
            CreditMessage =
                "Correct any invalid values and run the analysis again.";

            return Page();
        }

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage =
                "Create a free account to run a repair analysis.";

            return Page();
        }

        /*
         * If the customer already used a credit for
         * PonyUp's repair estimate, do not charge a
         * second credit for the decision immediately
         * following that estimate.
         */
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
                    await _usageCreditService.GetStatusAsync(
                        User);

                return Page();
            }
        }

        Result =
            _repairScoringService.Analyze(
                Input);

        EstimateCreditConsumed = false;

        CreditStatus =
            await _usageCreditService.GetStatusAsync(
                User);

        WorkflowStage = 3;

        return Page();
    }

    public async Task<IActionResult>
        OnPostChangeEstimateAsync()
    {
        WorkflowStage = 1;

        Result = null;

        await LoadPageStateAsync();

        return Page();
    }

    public async Task<IActionResult>
        OnPostEditAnalysisAsync()
    {
        WorkflowStage = 2;

        Result = null;

        await LoadPageStateAsync();

        return Page();
    }

    private async Task LoadPageStateAsync()
    {
        RepairTypes =
            _repairCostEstimatorService.GetRepairTypes();

        CreditStatus =
            await _usageCreditService.GetStatusAsync(
                User);
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
            Input.VehicleYear =
                decoded.Year.Value;
        }

        if (!string.IsNullOrWhiteSpace(
                decoded.Make))
        {
            Input.VehicleMake =
                decoded.Make;
        }

        if (!string.IsNullOrWhiteSpace(
                decoded.Model))
        {
            Input.VehicleModel =
                decoded.Model;
        }
    }
}
