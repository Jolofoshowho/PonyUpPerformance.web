using System;
using System.Collections.Generic;

namespace PonyUpPerformance.Web.Models
{
    public class BuyDecisionResult
    {
        public DecisionType Decision { get; set; }

        public int DecisionScore { get; set; }

        public int RiskScore { get; set; }

        public string RiskLevel { get; set; } = string.Empty;

        public int ConfidenceScore { get; set; }

        public string ConfidenceLevel { get; set; } = string.Empty;

        public decimal MaximumRecommendedPrice { get; set; }

        public decimal FairPurchasePrice { get; set; }

        public decimal SuggestedFirstOffer { get; set; }

        public decimal TotalAcquisitionCost { get; set; }

        public decimal EstimatedEquity { get; set; }

        public decimal EstimatedProfitPotential { get; set; }

        public int PositiveAdjustmentTotal { get; set; }

        public int NegativeAdjustmentTotal { get; set; }

        public List<BuyScoreAdjustment> ScoreAdjustments { get; set; } = new();

        public List<string> Strengths { get; set; } = new();

        public List<string> Concerns { get; set; } = new();

        public List<string> NextSteps { get; set; } = new();

        public string DecisionSummary { get; set; } = string.Empty;

        public string RiskExplanation { get; set; } = string.Empty;

        public string ConfidenceExplanation { get; set; } = string.Empty;

        public string Reasoning { get; set; } = string.Empty;

        public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class BuyScoreAdjustment
    {
        public string Influencer { get; set; } = string.Empty;

        public int Adjustment { get; set; }

        public int MaximumAdjustment { get; set; }

        public string Explanation { get; set; } = string.Empty;
    }
}
