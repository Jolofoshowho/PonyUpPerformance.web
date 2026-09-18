using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services
{
    public class AnalysisHistoryService
    {
        private readonly ApplicationDbContext
            _dbContext;

        private readonly UserManager<ApplicationUser>
            _userManager;

        public AnalysisHistoryService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext =
                dbContext;

            _userManager =
                userManager;
        }

        public async Task SaveRepairAnalysisAsync(
            ClaimsPrincipal userPrincipal,
            RepairDecisionInput input,
            RepairCostEstimateInput estimateInput,
            RepairCostEstimateResult estimateResult,
            DecisionResult result)
        {
            ApplicationUser? user =
                await _userManager.GetUserAsync(
                    userPrincipal);

            if (user == null)
            {
                return;
            }

            AnalysisHistory history =
                new AnalysisHistory
                {
                    UserId =
                        user.Id,

                    AnalysisType =
                        "Repair",

                    VehicleYear =
                        input.VehicleYear ?? 0,

                    VehicleMake =
                        input.VehicleMake
                        ?? string.Empty,

                    VehicleModel =
                        input.VehicleModel
                        ?? string.Empty,

                    Mileage =
                        input.Mileage ?? 0,

                    RepairType =
                        estimateInput.RepairType
                        ?? string.Empty,

                    LowEstimate =
                        estimateResult.LowEstimate,

                    ExpectedEstimate =
                        estimateResult.ExpectedEstimate,

                    HighEstimate =
                        estimateResult.HighEstimate,

                    VehicleValue =
                        input.VehicleValue ?? 0m,

                    VehicleCondition =
                        input.Condition ==
                        MechanicalCondition.NotProvided
                            ? string.Empty
                            : input.Condition.ToString(),

                    PlannedOwnershipYears =
                        input.OwnershipYears ?? 0,

                    Recommendation =
                        result.Recommendation,

                    ConfidenceScore =
                        result.ConfidenceScore,

                    RiskLevel =
                        result.RiskLevel,

                    FinancialImpact =
                        result.FinancialImpact,

                    Reasoning =
                        result.Reasoning,

                    CreatedOn =
                        DateTime.UtcNow
                };

            _dbContext.AnalysisHistories.Add(
                history);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<AnalysisHistory>>
            GetUserHistoryAsync(
                ClaimsPrincipal userPrincipal)
        {
            ApplicationUser? user =
                await _userManager.GetUserAsync(
                    userPrincipal);

            if (user == null)
            {
                return new List<AnalysisHistory>();
            }

            return await _dbContext.AnalysisHistories
                .Where(
                    history =>
                        history.UserId ==
                        user.Id)
                .OrderByDescending(
                    history =>
                        history.CreatedOn)
                .ToListAsync();
        }
    }
}
