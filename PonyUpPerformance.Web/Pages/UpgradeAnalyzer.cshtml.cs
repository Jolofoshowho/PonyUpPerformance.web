using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Pages
{
    public class UpgradeAnalyzerModel : PageModel
    {
        [BindProperty]
        public UpgradeDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; set; }

        public void OnGet()
        {
        }

        public void OnPost()
        {
            int score = 50;

            if (Input.UpgradeCost <= 500) score += 15;
            else if (Input.UpgradeCost <= 1500) score += 8;
            else if (Input.UpgradeCost <= 3000) score -= 5;
            else score -= 20;

            if (Input.ValueAdded >= Input.UpgradeCost) score += 25;
            else if (Input.ValueAdded >= Input.UpgradeCost * 0.5m) score += 10;
            else score -= 15;

            if (Input.HorsepowerGain >= 100) score += 15;
            else if (Input.HorsepowerGain >= 50) score += 8;
            else if (Input.HorsepowerGain >= 20) score += 3;

            if (Input.ReliabilityImpact >= 50) score += 15;
            else if (Input.ReliabilityImpact >= 0) score += 5;
            else if (Input.ReliabilityImpact >= -50) score -= 15;
            else score -= 30;

            score = Math.Max(0, Math.Min(100, score));

            string decision =
                score >= 75 ? "PONY UP" :
                score >= 50 ? "PLAN CAREFULLY" :
                "WALK AWAY";

            string summary =
                score >= 75
                    ? "The upgrade looks worth the money. It adds enough value, performance, or benefit to justify moving forward."
                    : score >= 50
                        ? "The upgrade may be worth doing, but only with a tighter budget, better parts plan, or lower reliability risk."
                        : "The upgrade does not currently justify the spend. Too much money is going out for too little return.";

            Result = new BuyDecisionResult
            {
                Score = score,
                Decision = decision,
                Summary = summary
            };
        }
    }
}
