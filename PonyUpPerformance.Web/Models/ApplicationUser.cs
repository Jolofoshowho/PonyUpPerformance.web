using Microsoft.AspNetCore.Identity;

namespace PonyUpPerformance.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int RemainingCredits { get; set; } = 1;

        public bool HasUsedFreeAnalysis { get; set; }

        public string CurrentPlan { get; set; } = "Free";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}