namespace PonyUpPerformance.Web.Models;

public class UpgradeDecisionInput
{
    public int VehicleYear { get; set; }

    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public decimal UpgradeCost { get; set; }

    public decimal VehicleValue { get; set; }

    public string Goal { get; set; } = string.Empty;

    public bool IsDailyDriver { get; set; }

    public string ExpectedResaleImpact { get; set; } = string.Empty;
}