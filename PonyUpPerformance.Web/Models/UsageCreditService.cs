using Microsoft.AspNetCore.Identity;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services
{
    public class UsageCreditService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        private static readonly string[] OwnerEmails =
        {
            "lopezkb258@gmail.com",
            "lopez2kb258@gmail.com"
        };

        public UsageCreditService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<UsageCreditStatus> GetStatusAsync(ClaimsPrincipal userPrincipal)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(userPrincipal);

            if (user == null)
            {
                return new UsageCreditStatus
                {
                    IsLoggedIn = false,
                    CanRunAnalysis = false,
                    RemainingCredits = 0,
                    CurrentPlan = "Guest",
                    Message = "Create a free account to unlock your first full analysis."
                };
            }

            if (IsOwner(user))
            {
                return new UsageCreditStatus
                {
                    IsLoggedIn = true,
                    CanRunAnalysis = true,
                    RemainingCredits = int.MaxValue,
                    CurrentPlan = "Owner",
                    Message = "Owner access active."
                };
            }

            bool canRun = user.RemainingCredits > 0 || user.CurrentPlan == "Unlimited";

            return new UsageCreditStatus
            {
                IsLoggedIn = true,
                CanRunAnalysis = canRun,
                RemainingCredits = user.RemainingCredits,
                CurrentPlan = user.CurrentPlan,
                Message = canRun
                    ? "Analysis available."
                    : "You are out of analysis credits. Upgrade to continue."
            };
        }

        public async Task<bool> ConsumeCreditAsync(ClaimsPrincipal userPrincipal, string analysisType)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(userPrincipal);

            if (user == null)
            {
                return false;
            }

            bool owner = IsOwner(user);

            if (!owner && user.CurrentPlan != "Unlimited")
            {
                if (user.RemainingCredits <= 0)
                {
                    return false;
                }

                user.RemainingCredits -= 1;
            }

            if (!owner && user.CurrentPlan == "Free")
            {
                user.HasUsedFreeAnalysis = true;
            }

            _dbContext.AnalysisUsages.Add(new AnalysisUsage
            {
                UserId = user.Id,
                AnalysisDate = DateTime.UtcNow,
                AnalysisType = analysisType,
                CreditsConsumed = owner || user.CurrentPlan == "Unlimited" ? 0 : 1
            });

            await _userManager.UpdateAsync(user);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static bool IsOwner(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return false;
            }

            return OwnerEmails.Any(ownerEmail =>
                string.Equals(ownerEmail, user.Email, StringComparison.OrdinalIgnoreCase));
        }
    }
}