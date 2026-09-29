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

        public async Task SaveAnalysisAsync(
            ClaimsPrincipal userPrincipal,
            string analysisType,
            int? vehicleYear,
            string? vehicleMake,
            string? vehicleModel,
            int? mileage,
            decimal? vehicleValue,
            DecisionResult result,
            string? repairType = null,
            decimal? lowEstimate = null,
            decimal? expectedEstimate = null,
            decimal? highEstimate = null,
            string? vehicleCondition = null,
            int? plannedOwnershipYears = null)
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
                        string.IsNullOrWhiteSpace(analysisType)
                            ? "Analysis"
                            : analysisType.Trim(),

                    VehicleYear =
                        vehicleYear ?? 0,

                    VehicleMake =
                        vehicleMake?.Trim()
                        ?? string.Empty,

                    VehicleModel =
                        vehicleModel?.Trim()
                        ?? string.Empty,

                    Mileage =
                        mileage ?? 0,

                    RepairType =
                        repairType?.Trim()
                        ?? string.Empty,

                    LowEstimate =
                        lowEstimate ?? 0m,

                    ExpectedEstimate =
                        expectedEstimate ?? 0m,

                    HighEstimate =
                        highEstimate ?? 0m,

                    VehicleValue =
                        vehicleValue ?? 0m,

                    VehicleCondition =
                        vehicleCondition?.Trim()
                        ?? string.Empty,

                    PlannedOwnershipYears =
                        plannedOwnershipYears ?? 0,

                    Recommendation =
                        result.Recommendation
                        ?? string.Empty,

                    ConfidenceScore =
                        result.ConfidenceScore,

                    RiskLevel =
                        result.RiskLevel
                        ?? string.Empty,

                    FinancialImpact =
                        result.FinancialImpact
                        ?? string.Empty,

                    Reasoning =
                        result.Reasoning
                        ?? string.Empty,

                    CreatedOn =
                        DateTime.UtcNow
                };

            _dbContext.AnalysisHistories.Add(
                history);

            await _dbContext.SaveChangesAsync();
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
