namespace PonyUpPerformance.Web.Models;

public class TradeDecisionResult : DecisionResult
{
    public decimal? YourAdjustedValue { get; set; }

    public decimal? TheirAdjustedValue { get; set; }

    public decimal? NetTradePosition { get; set; }
}
