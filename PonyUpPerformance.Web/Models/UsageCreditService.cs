using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services
{
    public class UsageCreditService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly PlanEntitlementService _planEntitlementService;

        private static readonly string[] OwnerEmails =
        {
            "lopezkb258@gmail.com",
            "lopez2kb258@gmail.com"
        };

        public UsageCreditService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            PlanEntitlementService planEntitlementService)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _planEntitlementService = planEntitlementService;
        }

        public async Task<UsageCreditStatus> GetStatusAsync(
            ClaimsPrincipal userPrincipal)
        {
            ApplicationUser? user =
                await _userManager.GetUserAsync(
                    userPrincipal);

            if (user == null)
            {
                return new UsageCreditStatus
                {
                    IsLoggedIn = false,
                    CanRunAnalysis = false,
                    RemainingCredits = 0,
                    OneTimeCredits = 0,
                    SubscriptionCredits = 0,
                    CurrentPlan = "Guest",
                    PlanKey = "guest",
                    Message =
                        "Create a free account to unlock your first full analysis."
                };
            }

            await _planEntitlementService
                .RefreshMonthlyEntitlementsAsync(
                    user);

            bool owner =
                IsOwner(user);

            bool hasQuickPackPurchase =
                !owner &&
                await _dbContext.StripePurchases
                    .AnyAsync(x =>
                        x.UserId == user.Id &&
                        x.PlanKey == PonyUpPlanCatalog.QuickPackKey);

            PonyUpPlanAccess access =
                PonyUpPlanCatalog.Resolve(
                    user.CurrentPlan,
                    owner,
                    hasQuickPackPurchase);

            int oneTimeCredits =
                Math.Max(
                    0,
                    user.RemainingCredits);

            int subscriptionCredits =
                Math.Max(
                    0,
                    user.SubscriptionCredits);

            int totalCredits =
                access.UnlimitedStandardAnalyses
                    ? int.MaxValue
                    : oneTimeCredits +
                      subscriptionCredits;

            bool canRun =
                access.UnlimitedStandardAnalyses ||
                totalCredits > 0;

            return new UsageCreditStatus
            {
                IsLoggedIn = true,
                CanRunAnalysis = canRun,
                RemainingCredits = totalCredits,
                OneTimeCredits = oneTimeCredits,
                SubscriptionCredits = subscriptionCredits,
                CurrentPlan = access.DisplayName,
                PlanKey = access.Key,
                UnlimitedStandardAnalyses =
                    access.UnlimitedStandardAnalyses,
                Has3DGarage =
                    access.Has3DGarage,
                CanCustomizeExterior =
                    access.CanCustomizeExterior,
                CanCustomizeInterior =
                    access.CanCustomizeInterior,
                CanCustomizeWheels =
                    access.CanCustomizeWheels,
                CanUseSpecialTrims =
                    access.CanUseSpecialTrims,
                RevUpReportsPerBillingCycle =
                    access.RevUpReportsPerBillingCycle,
                RevUpReportsRemaining =
                    access.UnlimitedRevUpReports
                        ? int.MaxValue
                        : Math.Max(
                            0,
                            user.RevUpReportsRemaining),
                UnlimitedRevUpReports =
                    access.UnlimitedRevUpReports,
                Message = canRun
                    ? "Analysis available."
                    : "You are out of analysis credits. Upgrade to continue."
            };
        }

        public async Task<bool> ConsumeCreditAsync(
            ClaimsPrincipal userPrincipal,
            string analysisType)
        {
            ApplicationUser? user =
                await _userManager.GetUserAsync(
                    userPrincipal);

            if (user == null)
            {
                return false;
            }

            await _planEntitlementService
                .RefreshMonthlyEntitlementsAsync(
                    user);

            bool owner =
                IsOwner(user);

            PonyUpPlanAccess access =
                PonyUpPlanCatalog.Resolve(
                    user.CurrentPlan,
                    owner);

            bool consumedCredit =
                false;

            if (!access.UnlimitedStandardAnalyses)
            {
                if (user.SubscriptionCredits > 0)
                {
                    user.SubscriptionCredits -= 1;
                    consumedCredit = true;
                }
                else if (user.RemainingCredits > 0)
                {
                    user.RemainingCredits -= 1;
                    consumedCredit = true;
                }
                else
                {
                    return false;
                }
            }

            if (!owner &&
                access.Key == PonyUpPlanCatalog.FreeKey &&
                consumedCredit)
            {
                user.HasUsedFreeAnalysis = true;
            }

            _dbContext.AnalysisUsages.Add(
                new AnalysisUsage
                {
                    UserId = user.Id,
                    AnalysisDate = DateTime.UtcNow,
                    AnalysisType = analysisType,
                    CreditsConsumed =
                        access.UnlimitedStandardAnalyses
                            ? 0
                            : 1
                });

            await _userManager.UpdateAsync(
                user);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static bool IsOwner(
            ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(
                user.Email))
            {
                return false;
            }

            return OwnerEmails.Any(
                ownerEmail =>
                    string.Equals(
                        ownerEmail,
                        user.Email,
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
