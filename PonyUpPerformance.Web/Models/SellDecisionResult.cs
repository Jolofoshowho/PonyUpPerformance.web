namespace PonyUpPerformance.Web.Models;

public class SellDecisionResult : DecisionResult
{
    public string WeightingProfile { get; set; }
        = string.Empty;

    public decimal? SalePriceVsMarketPercent { get; set; }

    public decimal? SalePriceVsMarketDifference { get; set; }
}
