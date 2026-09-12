using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public interface ITradeScoringService
{
    TradeDecisionResult Analyze(
        TradeDecisionInput input);
}
