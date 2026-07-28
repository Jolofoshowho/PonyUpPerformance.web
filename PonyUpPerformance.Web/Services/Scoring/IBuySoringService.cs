using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring
{
    public interface IBuyScoringService
    {
        BuyDecisionResult Analyze(BuyDecisionInput input);
    }
}
