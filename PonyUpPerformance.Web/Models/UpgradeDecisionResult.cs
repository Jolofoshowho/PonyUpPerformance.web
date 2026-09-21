namespace PonyUpPerformance.Web.Models;

public class UpgradeDecisionResult : DecisionResult
{
    public string WeightingProfile { get; set; }
        = string.Empty;

    public decimal? UpgradeCostVsVehicleValuePercent { get; set; }

    public decimal? ValueRecoveryPercent { get; set; }

    public decimal? ProjectedVehicleValue { get; set; }

    public decimal? NetValueImpact { get; set; }
}
