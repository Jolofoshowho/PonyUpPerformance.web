namespace PonyUpPerformance.Web.Models;

public class SellDecisionInput
{
    public int Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int Mileage { get; set; }
    public decimal ExpectedSalePrice { get; set; }
    public decimal EstimatedRepairCost { get; set; }
    public decimal MarketValue { get; set; }
}
