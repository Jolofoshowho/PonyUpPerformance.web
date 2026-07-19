namespace PonyUpPerformance.Web.Models
{
    public class ReliabilityResult
    {
        public int Score { get; set; }

        public string Trend { get; set; } = "";

        public string Summary { get; set; } = "";

        public List<string> Strengths { get; set; } = new();

        public List<string> CommonFailures { get; set; } = new();

        public List<string> MaintenanceRecommendations { get; set; } = new();

        public List<string> Sources { get; set; } = new();
    }
}
