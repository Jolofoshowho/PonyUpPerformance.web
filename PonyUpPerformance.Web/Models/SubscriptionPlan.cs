namespace PonyUpPerformance.Web.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int AnalysisCredits { get; set; }

        public bool IsRecurring { get; set; }

        public bool IsActive { get; set; }
    }
}