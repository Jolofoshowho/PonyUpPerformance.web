using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class RepairAnalyzerModel : PageModel
{
    private const string EstimateCreditKey = "RepairEstimateCreditPaid";

    private readonly IRepairScoringService _repairScoringService;
    private readonly RepairCostEstimatorService _repairCostEstimatorService;
    private readonly UsageCreditService _usageCreditService;

    public RepairAnalyzerModel(
        IRepairScoringService repairScoringService,
        RepairCostEstimatorService repairCostEstimatorService,
        UsageCreditService usageCreditService)
    {
        _repairScoringService = repairScoringService;
        _repairCostEstimatorService = repairCostEstimatorService;
        _usageCreditService = usageCreditService;
    }

    [BindProperty]
    public RepairDecisionInput Input { get; set; } = new();

    [BindProperty]
    public RepairCostEstimateInput EstimateInput { get; set; } = new();

    public RepairCostEstimateResult? EstimateResult { get; set; }

    public DecisionResult? Result { get; set; }

    public UsageCreditStatus CreditStatus { get; set; } = new();

    public string? CreditMessage { get; set; }

    public List<string> RepairTypes { get; set; } = new();

    public async Task OnGetAsync()
    {
        RepairTypes = _repairCostEstimatorService.GetRepairTypes();
        CreditStatus = await _usageCreditService.GetStatusAsync(User);
    }

    public async Task<IActionResult> OnPostEstimateAsync()
    {
        RepairTypes = _repairCostEstimatorService.GetRepairTypes();
        CreditStatus = await _usageCreditService.GetStatusAsync(User);

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage =
                "Create a free account to unlock your first repair cost estimate.";

            return Page();
        }

        if (!CreditStatus.CanRunAnalysis)
        {
            CreditMessage =
                "You are out of analysis credits. Choose a plan to continue.";

            return Page();
        }

        EstimateInput.VehicleYear = Input.VehicleYear;
        EstimateInput.VehicleMake = Input.VehicleMake;
        EstimateInput.VehicleModel = Input.VehicleModel;

        EstimateResult =
            _repairCostEstimatorService.Estimate(EstimateInput);

        Input.RepairCost = EstimateResult.ExpectedEstimate;

        bool creditConsumed =
            await _usageCreditService.ConsumeCreditAsync(
                User,
                "Repair");

        if (!creditConsumed)
        {
            CreditMessage =
                "Unable to consume analysis credit. Please log in again or choose a plan.";

            EstimateResult = null;
            Input.RepairCost = 0;

            CreditStatus =
                await _usageCreditService.GetStatusAsync(User);

            return Page();
        }

        TempData[EstimateCreditKey] = "true";

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        RepairTypes = _repairCostEstimatorService.GetRepairTypes();
        CreditStatus = await _usageCreditService.GetStatusAsync(User);

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage =
                "Create a free account to run a repair analysis.";

            return Page();
        }

        bool estimateWasCompleted =
            string.Equals(
                TempData[EstimateCreditKey]?.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (!estimateWasCompleted)
        {
            CreditMessage =
                "Estimate the repair cost before running the repair analysis.";

            return Page();
        }

        if (Input.RepairCost <= 0)
        {
            CreditMessage =
                "A valid repair cost is required before running the analysis.";

            return Page();
        }

        Result = _repairScoringService.Analyze(Input);

        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        return Page();
    }
}
