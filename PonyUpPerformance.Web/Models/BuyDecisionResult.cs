namespace PonyUpPerformance.Web.Models;

public class BuyDecisionResult : DecisionResult
{
    public decimal MaximumRecommendedPrice { get; set; }

    public decimal FairPurchasePrice { get; set; }

    public decimal SuggestedFirstOffer { get; set; }
}
