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

    public async Task<IActionResult> OnPostAsync()
    {
        RepairTypes = _repairCostEstimatorService.GetRepairTypes();
        CreditStatus = await _usageCreditService.GetStatusAsync(User);

        if (!CreditStatus.IsLoggedIn)
        {
            CreditMessage = "Create a free account to unlock your first full analysis.";
            return Page();
        }

        if (!CreditStatus.CanRunAnalysis)
        {
            CreditMessage = "You are out of analysis credits. Choose a plan to continue.";
            return Page();
        }

        EstimateInput.VehicleYear = Input.VehicleYear;
        EstimateInput.VehicleMake = Input.VehicleMake;
        EstimateInput.VehicleModel = Input.VehicleModel;

       if (Input.RepairCost <= 0)
{
    EstimateResult = _repairCostEstimatorService.Estimate(EstimateInput);
    Input.RepairCost = EstimateResult.ExpectedEstimate;
}

        Result = _repairScoringService.Analyze(Input);

        bool creditConsumed = await _usageCreditService.ConsumeCreditAsync(User, "Repair");

        if (!creditConsumed)
        {
            CreditMessage = "Unable to consume analysis credit. Please log in again or choose a plan.";
            Result = null;
            EstimateResult = null;
            CreditStatus = await _usageCreditService.GetStatusAsync(User);
            return Page();
        }

        CreditStatus = await _usageCreditService.GetStatusAsync(User);

        return Page();
    }
}
