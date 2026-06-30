namespace PonyUpPerformance.Web.Models;

public class TradeDecisionInput
{
    public int YourYear { get; set; }
    public string? YourMake { get; set; }
    public string? YourModel { get; set; }
    public int YourMileage { get; set; }
    public decimal YourValue { get; set; }

    public int TheirYear { get; set; }
    public string? TheirMake { get; set; }
    public string? TheirModel { get; set; }
    public int TheirMileage { get; set; }
    public decimal TheirValue { get; set; }

    public decimal EstimatedRepairCost { get; set; }
}
