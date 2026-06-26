using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public interface IRepairScoringService
{
    DecisionResult Analyze(RepairDecisionInput input);
}