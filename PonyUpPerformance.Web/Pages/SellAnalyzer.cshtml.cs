using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages;

public class SellAnalyzerModel : PageModel
{
    [BindProperty]
    public SellDecisionInput Input { get; set; } = new();

    public BuyDecisionResult? Result { get; set; }

    public void OnGet()
    {
    }

    public void OnPost()
    {
        int score = 50;

        decimal netAfterRepair = Input.ExpectedSalePrice - Input.EstimatedRepairCost;

        if (Input.Mileage <= 25000) score += 20;
        else if (Input.Mileage <= 50000) score += 12;
        else if (Input.Mileage <= 100000) score += 5;
        else if (Input.Mileage <= 150000) score -= 10;
        else if (Input.Mileage <= 200000) score -= 20;
        else score -= 30;

        if (Input.EstimatedRepairCost <= 500) score += 15;
        else if (Input.EstimatedRepairCost <= 1500) score += 5;
        else if (Input.EstimatedRepairCost <= 3000) score -= 15;
        else score -= 30;

        if (Input.MarketValue > 0 && netAfterRepair >= Input.MarketValue) score += 20;
        else if (Input.MarketValue > 0 && netAfterRepair >= Input.MarketValue * 0.85m) score += 10;
        else if (Input.MarketValue > 0 && netAfterRepair < Input.MarketValue * 0.70m) score -= 25;

        score = Math.Max(0, Math.Min(100, score));

        Result = new BuyDecisionResult
        {
            Score = score,
            Decision = score >= 75 ? "SELL NOW" : score >= 50 ? "NEGOTIATE / REPAIR FIRST" : "HOLD OR REWORK",
            Summary = score >= 75
                ? "The numbers support selling now."
                : score >= 50
                    ? "Selling may make sense, but repairs, pricing, or timing need to be tightened up."
                    : "The current sale path leaves too much money on the table."
        };
    }
}
