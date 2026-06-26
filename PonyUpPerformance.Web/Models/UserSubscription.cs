namespace PonyUpPerformance.Web.Models
{
    public class UserSubscription
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int SubscriptionPlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public decimal AmountPaid { get; set; }

        public DateTime PurchaseDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public bool IsActive { get; set; }
    }
}