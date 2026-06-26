using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services
{
    public class AnalysisHistoryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalysisHistoryService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task SaveRepairAnalysisAsync(
            ClaimsPrincipal userPrincipal,
            RepairDecisionInput input,
            RepairCostEstimateInput estimateInput,
            RepairCostEstimateResult estimateResult,
            DecisionResult result)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(userPrincipal);

            if (user == null)
            {
                return;
            }

            AnalysisHistory history = new AnalysisHistory
            {
                UserId = user.Id,
                AnalysisType = "Repair",

                VehicleYear = input.VehicleYear,
                VehicleMake = input.VehicleMake,
                VehicleModel = input.VehicleModel,
                Mileage = input.Mileage,

                RepairType = estimateInput.RepairType,
                LowEstimate = estimateResult.LowEstimate,
                ExpectedEstimate = estimateResult.ExpectedEstimate,
                HighEstimate = estimateResult.HighEstimate,

                VehicleValue = input.VehicleValue,
                VehicleCondition = input.Condition.ToString(),
                PlannedOwnershipYears = input.OwnershipYears,

                Recommendation = result.Recommendation.ToString(),
                ConfidenceScore = result.ConfidenceScore,
                RiskLevel = result.RiskLevel.ToString(),
                FinancialImpact = result.FinancialImpact.ToString(),
                Reasoning = result.Reasoning,

                CreatedOn = DateTime.UtcNow
            };

            _dbContext.AnalysisHistories.Add(history);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<AnalysisHistory>> GetUserHistoryAsync(ClaimsPrincipal userPrincipal)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(userPrincipal);

            if (user == null)
            {
                return new List<AnalysisHistory>();
            }

            return await _dbContext.AnalysisHistories
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
        }
    }
}