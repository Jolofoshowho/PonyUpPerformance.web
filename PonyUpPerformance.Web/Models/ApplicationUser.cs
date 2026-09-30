using Microsoft.AspNetCore.Identity;

namespace PonyUpPerformance.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Free/one-time credits (Free analysis + Quick Pack purchases).
        public int RemainingCredits { get; set; } = 1;

        // Recurring-plan credits. Pro resets this bucket each paid billing cycle.
        public int SubscriptionCredits { get; set; }

        public bool HasUsedFreeAnalysis { get; set; }

        public string CurrentPlan { get; set; } = "Free";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}