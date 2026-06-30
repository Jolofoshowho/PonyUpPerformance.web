namespace PonyUpPerformance.Web.Models;

public class UpgradeDecisionInput
{
    public int Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }

    public decimal CurrentValue { get; set; }
    public decimal UpgradeCost { get; set; }
    public decimal ValueAdded { get; set; }

    public int HorsepowerGain { get; set; }
    public int ReliabilityImpact { get; set; }
}
