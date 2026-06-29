using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class BuyAnalyzerModel : PageModel
    {
        [BindProperty]
        public BuyDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            int score = 100;

            if (Input.Mileage > 180000) score -= 25;
            else if (Input.Mileage > 120000) score -= 15;

            if (Input.EstimatedRepairCost > 2500) score -= 25;
            else if (Input.EstimatedRepairCost > 1000) score -= 12;

            decimal totalInvestment = Input.AskingPrice + Input.EstimatedRepairCost;

            if (Input.MarketValue > 0 && totalInvestment > Input.MarketValue)
                score -= 25;

            string decision =
                score >= 75 ? "PONY UP" :
                score >= 50 ? "NEGOTIATE" :
                "WALK AWAY";

            string summary =
                score >= 75
                    ? "The numbers look strong enough to move forward, assuming the title and inspection check out."
                    : score >= 50
                        ? "This could still be worth buying, but only if you negotiate the price or reduce the repair risk."
                        : "The risk is too high compared to the likely value. Walk away unless the deal changes hard.";

            Result = new BuyDecisionResult
            {
                Score = score,
                Decision = decision,
                Summary = summary
            };
        }
    }
}
