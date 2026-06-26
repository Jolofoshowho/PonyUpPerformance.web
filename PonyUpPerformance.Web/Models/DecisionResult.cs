namespace PonyUpPerformance.Web.Models;

public class DecisionResult
{
    public string Recommendation { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string FinancialImpact { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;

    public List<string> NextSteps { get; set; } = new();
}