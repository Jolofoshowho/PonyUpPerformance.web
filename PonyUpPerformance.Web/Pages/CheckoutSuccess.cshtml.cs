using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages
{
    public class CheckoutSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeCheckoutService _stripeCheckoutService;

        public string Message { get; set; } = "";

        public CheckoutSuccessModel(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            StripeCheckoutService stripeCheckoutService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _stripeCheckoutService = stripeCheckoutService;
        }

        public async Task OnGetAsync(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
            {
                Message = "Missing checkout session.";
                return;
            }

            bool alreadyProcessed = await _dbContext.StripePurchases
                .AnyAsync(x => x.StripeSessionId == session_id);

            if (alreadyProcessed)
            {
                Message = "Purchase already processed.";
                return;
            }

            var session = await _stripeCheckoutService.GetSessionAsync(session_id);

            if (session.PaymentStatus != "paid")
            {
                Message = "Payment has not completed.";
                return;
            }

            string userId = session.Metadata["UserId"];
            string planKey = session.Metadata["PlanKey"];

            ApplicationUser? user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                Message = "User not found.";
                return;
            }

            ApplyPlan(user, planKey);

            _dbContext.StripePurchases.Add(new StripePurchase
            {
                UserId = user.Id,
                StripeSessionId = session_id,
                PlanKey = planKey,
                CreatedOn = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            Message = "Payment complete. Your PonyUp credits are active.";
        }

        private static void ApplyPlan(ApplicationUser user, string planKey)
        {
            switch (planKey.ToLowerInvariant())
            {
                case "quickpack":
                    user.CurrentPlan = "Quick Pack";
                    user.RemainingCredits += 5;
                    break;

                case "pro":
                    user.CurrentPlan = "Pro";
                    user.RemainingCredits += 10;
                    break;

                case "unlimited":
                    user.CurrentPlan = "Unlimited";
                    user.RemainingCredits = 999999;
                    break;
            }
        }
    }
}