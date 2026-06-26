namespace PonyUpPerformance.Web.Models
{
    public class UsageCreditStatus
    {
        public bool IsLoggedIn { get; set; }

        public bool CanRunAnalysis { get; set; }

        public int RemainingCredits { get; set; }

        public string CurrentPlan { get; set; } = "Free";

        public string Message { get; set; } = "";
    }
}