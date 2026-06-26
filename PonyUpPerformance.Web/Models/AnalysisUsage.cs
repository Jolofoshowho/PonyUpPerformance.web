namespace PonyUpPerformance.Web.Models
{
    public class AnalysisUsage
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime AnalysisDate { get; set; }

        public string AnalysisType { get; set; } = string.Empty;

        public int CreditsConsumed { get; set; }
    }
}