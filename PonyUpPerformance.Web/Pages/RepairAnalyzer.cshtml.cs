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

    [TempData]
    public bool EstimateCreditConsumed { get; set; }

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
            "Create a free account to estimate the repair cost.";

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

    bool creditConsumed =
        await _usageCreditService.ConsumeCreditAsync(
            User,
            "Repair Cost Estimate");

    if (!creditConsumed)
    {
        CreditMessage =
            "Unable to consume an analysis credit. Please log in again or choose a plan.";

        EstimateResult = null;
        CreditStatus =
            await _usageCreditService.GetStatusAsync(User);

        return Page();
    }

    Input.RepairCost = EstimateResult.ExpectedEstimate;
    EstimateCreditConsumed = true;

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

    if (Input.RepairCost <= 0)
    {
        CreditMessage =
            "Enter a valid repair cost or use the repair cost estimator first.";

        return Page();
    }

    // If the estimator was not used, this manual analysis costs one credit.
    if (!EstimateCreditConsumed)
    {
        if (!CreditStatus.CanRunAnalysis)
        {
            CreditMessage =
                "You are out of analysis credits. Choose a plan to continue.";

            return Page();
        }

        bool creditConsumed =
            await _usageCreditService.ConsumeCreditAsync(
                User,
                "Repair");

        if (!creditConsumed)
        {
            CreditMessage =
                "Unable to consume an analysis credit. Please log in again or choose a plan.";

            CreditStatus =
                await _usageCreditService.GetStatusAsync(User);

            return Page();
        }
    }

    // Run the analysis using the estimated or manually entered repair cost.
    Result = _repairScoringService.Analyze(Input);

    // The paid estimate can authorize only this one analysis.
    EstimateCreditConsumed = false;

    CreditStatus =
        await _usageCreditService.GetStatusAsync(User);

    return Page();
}
}
