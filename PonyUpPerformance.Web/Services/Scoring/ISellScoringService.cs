using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public interface ISellScoringService
{
    SellDecisionResult Analyze(SellDecisionInput input);
}
