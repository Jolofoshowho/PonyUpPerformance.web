using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class RepairScoringService : IRepairScoringService
{
    public DecisionResult Analyze(RepairDecisionInput input)
    {
        if (input.VehicleValue <= 0)
        {
            return new DecisionResult
            {
                Recommendation = "Insufficient Data",
                ConfidenceScore = 0,
                RiskLevel = "Unknown",
                FinancialImpact = "Unknown",
                Reasoning = "Vehicle value must be greater than zero."
            };
        }

        decimal ratio = input.RepairCost / input.VehicleValue;

        string recommendation;

        if (ratio <= 0.25m)
            recommendation = "Repair";
        else if (ratio <= 0.50m)
            recommendation = "Lean Repair";
        else if (ratio <= 0.75m)
            recommendation = "Get Second Opinion";
        else if (ratio <= 1.00m)
            recommendation = "Lean Replace";
        else
            recommendation = "Do Not Repair";

        return new DecisionResult
        {
            Recommendation = recommendation,
            ConfidenceScore = 75,
            RiskLevel = ratio > 0.75m ? "High" : "Medium",
            FinancialImpact = ratio <= 0.50m ? "Positive" : "Negative",
            Reasoning =
                $"Repair cost equals {(ratio * 100):F0}% of vehicle value."
        };
    }
}