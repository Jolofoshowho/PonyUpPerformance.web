namespace PonyUpPerformance.Web.Models
{
    public class StripePurchase
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string StripeSessionId { get; set; } = "";
        public string PlanKey { get; set; } = "";
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}