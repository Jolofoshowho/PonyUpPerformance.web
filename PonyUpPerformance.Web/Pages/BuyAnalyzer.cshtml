using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages
{
    public class BuyAnalyzerModel : PageModel
    {
        private readonly IBuyScoringService _buyScoringService;

        public BuyAnalyzerModel(IBuyScoringService buyScoringService)
        {
            _buyScoringService = buyScoringService;
        }

        [BindProperty]
        public BuyDecisionInput Input { get; set; } = new();

        public BuyDecisionResult? Result { get; private set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Result = _buyScoringService.Analyze(Input);

            return Page();
        }
    }
}
