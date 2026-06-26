namespace PonyUpPerformance.Web.Models
{
    public class BuyDecisionResult
    {
        public string Recommendation { get; set; } = "";

        public int ConfidenceScore { get; set; }

        public string PurchaseRisk { get; set; } = "";

        public decimal PonyUpFairPurchasePrice { get; set; }

        public decimal RecommendedFirstOffer { get; set; }

        public decimal MaximumRecommendedPrice { get; set; }

        public decimal NegotiatingRoom { get; set; }

        public string PricePosition { get; set; } = "";

        public string MileageAssessment { get; set; } = "";

        public string RiskSummary { get; set; } = "";

        public string Reasoning { get; set; } = "";

        public List<string> Strengths { get; set; } = new();

        public List<string> Concerns { get; set; } = new();

        public List<string> NextSteps { get; set; } = new();
    }
}