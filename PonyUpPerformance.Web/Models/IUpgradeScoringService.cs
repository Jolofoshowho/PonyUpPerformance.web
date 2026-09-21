using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public interface IUpgradeScoringService
{
    UpgradeDecisionResult Analyze(
        UpgradeDecisionInput input);
}
