using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class TradeAnalyzerModel : PageModel
    {
        [BindProperty]
        public TradeDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            int score = 50;

            decimal netTheirValue = Input.TheirValue - Input.EstimatedRepairCost;
            decimal valueDifference = netTheirValue - Input.YourValue;

            if (valueDifference >= 2500) score += 25;
            else if (valueDifference >= 1000) score += 15;
            else if (valueDifference >= 0) score += 5;
            else if (valueDifference >= -1000) score -= 10;
            else score -= 25;

            if (Input.TheirMileage <= 50000) score += 15;
            else if (Input.TheirMileage <= 100000) score += 5;
            else if (Input.TheirMileage <= 150000) score -= 10;
            else if (Input.TheirMileage <= 200000) score -= 20;
            else score -= 35;

            if (Input.EstimatedRepairCost <= 500) score += 10;
            else if (Input.EstimatedRepairCost <= 1500) score += 0;
            else if (Input.EstimatedRepairCost <= 3000) score -= 15;
            else score -= 30;

            score = Math.Max(0, Math.Min(100, score));

            string decision =
                score >= 75 ? "PONY UP" :
                score >= 50 ? "NEGOTIATE" :
                "WALK AWAY";

            string summary =
                score >= 75
                    ? "The trade looks favorable. Their vehicle value and risk profile justify moving forward."
                    : score >= 50
                        ? "The trade may work, but only if the numbers improve or repair risk is reduced."
                        : "The trade does not currently make sense. Too much value or risk is stacked against you.";

            Result = new BuyDecisionResult
{
    ConfidenceScore = score,
    Recommendation = decision,
    Reasoning = summary
};
        }
    }
}
