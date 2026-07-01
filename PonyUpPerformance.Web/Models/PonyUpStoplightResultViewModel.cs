namespace PonyUpPerformance.Web.Models;

public class PonyUpStoplightResultViewModel
{
    public bool HasResult { get; set; }

    public int ConfidenceScore { get; set; }

    public string Recommendation { get; set; } = "READY";

    public string Reasoning { get; set; } = "Enter the information and run the analysis.";

    public string AnalysisType { get; set; } = "ANALYSIS STATUS";
}
