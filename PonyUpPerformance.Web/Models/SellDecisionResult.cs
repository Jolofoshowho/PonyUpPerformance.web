namespace PonyUpPerformance.Web.Models;

public class SellDecisionResult : DecisionResult
{
    public decimal? FairAskingPrice { get; set; }

    public decimal? CurrentOfferGap { get; set; }

    public decimal? RepairBreakEvenSalePrice { get; set; }

    public decimal? RepairCostToMarketPercent { get; set; }
}
