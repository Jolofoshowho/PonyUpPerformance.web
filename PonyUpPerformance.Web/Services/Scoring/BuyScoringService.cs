using System;
using System.Collections.Generic;
using System.Linq;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring
{
    public class BuyScoringService : IBuyScoringService
    {
        private const int BaseDecisionScore = 50;

        private const int PonyUpThreshold = 70;
        private const int CautionThreshold = 45;

        private const int MechanicalMaximum = 12;
        private const int AskingPriceMaximum = 10;
        private const int MileageMaximum = 8;
        private const int TitleMaximum = 8;
        private const int AccidentMaximum = 8;
        private const int IntendedUseMaximum = 4;

        public BuyDecisionResult Analyze(BuyDecisionInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            var adjustments = new List<BuyScoreAdjustment>
            {
                ScoreMechanicalCondition(input),
                ScoreAskingPrice(input),
                ScoreMileageForAge(input),
                ScoreTitleStatus(input),
                ScoreAccidentHistory(input),
                ScoreIntendedUse(input)
            };

            var positiveAdjustmentTotal = adjustments
                .Where(adjustment => adjustment.Adjustment > 0)
                .Sum(adjustment => adjustment.Adjustment);

            var negativeAdjustmentTotal = adjustments
                .Where(adjustment => adjustment.Adjustment < 0)
                .Sum(adjustment => adjustment.Adjustment);

            var decisionScore = Math.Clamp(
                BaseDecisionScore + positiveAdjustmentTotal + negativeAdjustmentTotal,
                0,
                100);

            var decision = DetermineDecision(decisionScore);

            var riskScore = CalculateRiskScore(input);
            var riskLevel = DetermineRiskLevel(riskScore);

            var confidenceScore = CalculateConfidenceScore(input);
            var confidenceLevel = DetermineConfidenceLevel(confidenceScore);

            var totalAcquisitionCost =
                input.AskingPrice + input.EstimatedRepairCost;

            var estimatedEquity =
                input.MarketValue - totalAcquisitionCost;

            var estimatedProfitPotential =
                Math.Max(0m, estimatedEquity);

            var maximumRecommendedPrice =
                CalculateMaximumRecommendedPrice(input);

            var fairPurchasePrice =
                CalculateFairPurchasePrice(input, maximumRecommendedPrice);

            var suggestedFirstOffer =
                CalculateSuggestedFirstOffer(input, fairPurchasePrice);

            var strengths = BuildStrengths(input, adjustments, estimatedEquity);
            var concerns = BuildConcerns(input, adjustments, estimatedEquity);
            var nextSteps = BuildNextSteps(input, decision, riskLevel);

            return new BuyDecisionResult
            {
                Decision = decision,
                DecisionScore = decisionScore,

                RiskScore = riskScore,
                RiskLevel = riskLevel,

                ConfidenceScore = confidenceScore,
                ConfidenceLevel = confidenceLevel,

                MaximumRecommendedPrice = RoundCurrency(maximumRecommendedPrice),
                FairPurchasePrice = RoundCurrency(fairPurchasePrice),
                SuggestedFirstOffer = RoundCurrency(suggestedFirstOffer),

                TotalAcquisitionCost = RoundCurrency(totalAcquisitionCost),
                EstimatedEquity = RoundCurrency(estimatedEquity),
                EstimatedProfitPotential = RoundCurrency(estimatedProfitPotential),

                PositiveAdjustmentTotal = positiveAdjustmentTotal,
                NegativeAdjustmentTotal = negativeAdjustmentTotal,
                ScoreAdjustments = adjustments,

                Strengths = strengths,
                Concerns = concerns,
                NextSteps = nextSteps,

                DecisionSummary = BuildDecisionSummary(decision),
                RiskExplanation = BuildRiskExplanation(input, riskScore),
                ConfidenceExplanation = BuildConfidenceExplanation(input),
                Reasoning = BuildReasoning(
                    decisionScore,
                    decision,
                    adjustments,
                    totalAcquisitionCost,
                    estimatedEquity),

                AnalyzedAtUtc = DateTime.UtcNow
            };
        }

        private static BuyScoreAdjustment ScoreMechanicalCondition(
            BuyDecisionInput input)
        {
            var repairRatio = input.MarketValue > 0
                ? input.EstimatedRepairCost / input.MarketValue
                : 1m;

            var conditionAdjustment = input.MechanicalCondition switch
            {
                MechanicalCondition.Excellent => 8,
                MechanicalCondition.Good => 5,
                MechanicalCondition.Fair => 0,
                MechanicalCondition.Poor => -6,
                MechanicalCondition.Severe => -10,
                _ => 0
            };

            var repairAdjustment = repairRatio switch
            {
                <= 0.02m => 4,
                <= 0.05m => 2,
                <= 0.10m => 0,
                <= 0.20m => -4,
                <= 0.35m => -8,
                _ => -12
            };

            var adjustment = Math.Clamp(
                conditionAdjustment + repairAdjustment,
                -MechanicalMaximum,
                MechanicalMaximum);

            var explanation =
                $"Mechanical condition is {FormatEnum(input.MechanicalCondition)}. " +
                $"Estimated repairs equal {repairRatio:P0} of market value.";

            return CreateAdjustment(
                "Mechanical Condition vs Needed Repairs",
                adjustment,
                MechanicalMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreAskingPrice(
            BuyDecisionInput input)
        {
            if (input.MarketValue <= 0)
            {
                return CreateAdjustment(
                    "Asking Price vs Market",
                    0,
                    AskingPriceMaximum,
                    "Market value was unavailable, so price position could not be scored.");
            }

            var priceDifferenceRatio =
                (input.MarketValue - input.AskingPrice) / input.MarketValue;

            var adjustment = priceDifferenceRatio switch
            {
                >= 0.25m => 10,
                >= 0.15m => 8,
                >= 0.10m => 6,
                >= 0.05m => 3,
                >= -0.03m => 0,
                >= -0.08m => -3,
                >= -0.15m => -6,
                >= -0.25m => -8,
                _ => -10
            };

            var explanation = priceDifferenceRatio >= 0
                ? $"The asking price is {priceDifferenceRatio:P0} below estimated market value."
                : $"The asking price is {Math.Abs(priceDifferenceRatio):P0} above estimated market value.";

            return CreateAdjustment(
                "Asking Price vs Market",
                adjustment,
                AskingPriceMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreMileageForAge(
            BuyDecisionInput input)
        {
            var currentYear = DateTime.UtcNow.Year;
            var vehicleAge = Math.Max(1, currentYear - input.Year);
            var expectedMileage = vehicleAge * 12_000m;

            var mileageRatio = expectedMileage > 0
                ? input.Mileage / expectedMileage
                : 1m;

            var adjustment = mileageRatio switch
            {
                <= 0.50m => 8,
                <= 0.70m => 6,
                <= 0.85m => 4,
                <= 1.00m => 2,
                <= 1.15m => 0,
                <= 1.35m => -3,
                <= 1.60m => -6,
                _ => -8
            };

            var explanation =
                $"The vehicle averages approximately " +
                $"{Math.Round(input.Mileage / (decimal)vehicleAge):N0} miles per year " +
                $"compared with a 12,000-mile annual benchmark.";

            return CreateAdjustment(
                "Mileage vs Age",
                adjustment,
                MileageMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreTitleStatus(
            BuyDecisionInput input)
        {
            var adjustment = input.TitleStatus switch
            {
                TitleStatus.Clean => 8,
                TitleStatus.Rebuilt => -4,
                TitleStatus.Salvage => -7,
                TitleStatus.Flood => -8,
                _ => 0
            };

            var explanation = input.TitleStatus switch
            {
                TitleStatus.Clean =>
                    "The vehicle has a clean title.",

                TitleStatus.Rebuilt =>
                    "A rebuilt title can reduce resale value and financing or insurance options.",

                TitleStatus.Salvage =>
                    "A salvage title creates substantial safety, resale, insurance, and registration risk.",

                TitleStatus.Flood =>
                    "A flood title presents severe long-term electrical, corrosion, and reliability risk.",

                _ =>
                    "Title status was not provided."
            };

            return CreateAdjustment(
                "Title Status",
                adjustment,
                TitleMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreAccidentHistory(
            BuyDecisionInput input)
        {
            var adjustment = input.AccidentHistory switch
            {
                AccidentHistory.None => 8,
                AccidentHistory.Minor => 2,
                AccidentHistory.Moderate => -3,
                AccidentHistory.Major => -8,
                _ => 0
            };

            var explanation = input.AccidentHistory switch
            {
                AccidentHistory.None =>
                    "No known accident history was reported.",

                AccidentHistory.Minor =>
                    "Minor accident history has a limited effect when repairs are properly documented.",

                AccidentHistory.Moderate =>
                    "Moderate accident history may affect structural integrity, alignment, and resale value.",

                AccidentHistory.Major =>
                    "Major accident history creates significant structural, safety, and resale concerns.",

                _ =>
                    "Accident history was not provided."
            };

            return CreateAdjustment(
                "Accident History",
                adjustment,
                AccidentMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreIntendedUse(
            BuyDecisionInput input)
        {
            var adjustment = input.IntendedUse switch
            {
                IntendedUse.DailyDriver => 2,
                IntendedUse.WorkVehicle => 1,
                IntendedUse.FamilyVehicle => 2,
                IntendedUse.ProjectVehicle => 0,
                IntendedUse.PerformanceBuild => -1,
                _ => 0
            };

            if (input.IntendedUse is IntendedUse.DailyDriver
                or IntendedUse.WorkVehicle
                or IntendedUse.FamilyVehicle)
            {
                if (input.MechanicalCondition == MechanicalCondition.Excellent)
                {
                    adjustment += 2;
                }
                else if (input.MechanicalCondition == MechanicalCondition.Poor)
                {
                    adjustment -= 3;
                }
                else if (input.MechanicalCondition == MechanicalCondition.Severe)
                {
                    adjustment -= 4;
                }
            }

            if (input.IntendedUse is IntendedUse.ProjectVehicle
                or IntendedUse.PerformanceBuild)
            {
                if (input.AskingPrice + input.EstimatedRepairCost
                    <= input.MarketValue * 0.80m)
                {
                    adjustment += 2;
                }
            }

            adjustment = Math.Clamp(
                adjustment,
                -IntendedUseMaximum,
                IntendedUseMaximum);

            var explanation =
                $"The vehicle is being evaluated as a " +
                $"{FormatEnum(input.IntendedUse)}.";

            return CreateAdjustment(
                "Intended Use",
                adjustment,
                IntendedUseMaximum,
                explanation);
        }

        private static int CalculateRiskScore(BuyDecisionInput input)
        {
            var risk = 0;

            risk += input.MechanicalCondition switch
            {
                MechanicalCondition.Excellent => 2,
                MechanicalCondition.Good => 6,
                MechanicalCondition.Fair => 14,
                MechanicalCondition.Poor => 24,
                MechanicalCondition.Severe => 32,
                _ => 12
            };

            risk += input.TitleStatus switch
            {
                TitleStatus.Clean => 0,
                TitleStatus.Rebuilt => 12,
                TitleStatus.Salvage => 22,
                TitleStatus.Flood => 28,
                _ => 10
            };

            risk += input.AccidentHistory switch
            {
                AccidentHistory.None => 0,
                AccidentHistory.Minor => 5,
                AccidentHistory.Moderate => 13,
                AccidentHistory.Major => 22,
                _ => 8
            };

            if (input.MarketValue > 0)
            {
                var repairRatio =
                    input.EstimatedRepairCost / input.MarketValue;

                risk += repairRatio switch
                {
                    <= 0.05m => 0,
                    <= 0.10m => 4,
                    <= 0.20m => 9,
                    <= 0.35m => 14,
                    _ => 18
                };

                var totalCostRatio =
                    (input.AskingPrice + input.EstimatedRepairCost)
                    / input.MarketValue;

                risk += totalCostRatio switch
                {
                    <= 0.80m => 0,
                    <= 0.95m => 3,
                    <= 1.05m => 7,
                    <= 1.20m => 12,
                    _ => 16
                };
            }

            return Math.Clamp(risk, 0, 100);
        }

        private static int CalculateConfidenceScore(
            BuyDecisionInput input)
        {
            var confidence = 40;

            if (!string.IsNullOrWhiteSpace(input.Vin))
            {
                confidence += 15;
            }

            if (!string.IsNullOrWhiteSpace(input.Trim))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(input.Engine))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(input.Transmission))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(input.Drivetrain))
            {
                confidence += 5;
            }

            if (input.MechanicalCondition != MechanicalCondition.NotProvided)
            {
                confidence += 8;
            }

            if (input.TitleStatus != TitleStatus.NotProvided)
            {
                confidence += 6;
            }

            if (input.AccidentHistory != AccidentHistory.NotProvided)
            {
                confidence += 6;
            }

            if (input.IntendedUse != IntendedUse.NotProvided)
            {
                confidence += 5;
            }

            return Math.Clamp(confidence, 0, 100);
        }

        private static decimal CalculateMaximumRecommendedPrice(
            BuyDecisionInput input)
        {
            var titleReserve = input.TitleStatus switch
            {
                TitleStatus.Clean => 0m,
                TitleStatus.Rebuilt => input.MarketValue * 0.12m,
                TitleStatus.Salvage => input.MarketValue * 0.25m,
                TitleStatus.Flood => input.MarketValue * 0.35m,
                _ => input.MarketValue * 0.05m
            };

            var accidentReserve = input.AccidentHistory switch
            {
                AccidentHistory.None => 0m,
                AccidentHistory.Minor => input.MarketValue * 0.03m,
                AccidentHistory.Moderate => input.MarketValue * 0.08m,
                AccidentHistory.Major => input.MarketValue * 0.18m,
                _ => input.MarketValue * 0.04m
            };

            var contingencyReserve =
                Math.Max(500m, input.EstimatedRepairCost * 0.20m);

            return Math.Max(
                0m,
                input.MarketValue
                - input.EstimatedRepairCost
                - contingencyReserve
                - titleReserve
                - accidentReserve);
        }

        private static decimal CalculateFairPurchasePrice(
            BuyDecisionInput input,
            decimal maximumRecommendedPrice)
        {
            var negotiationReserve =
                Math.Max(500m, input.MarketValue * 0.05m);

            return Math.Max(
                0m,
                Math.Min(
                    maximumRecommendedPrice,
                    input.MarketValue
                    - input.EstimatedRepairCost
                    - negotiationReserve));
        }

        private static decimal CalculateSuggestedFirstOffer(
            BuyDecisionInput input,
            decimal fairPurchasePrice)
        {
            var openingDiscount =
                Math.Max(500m, fairPurchasePrice * 0.08m);

            var firstOffer =
                Math.Max(0m, fairPurchasePrice - openingDiscount);

            return Math.Min(firstOffer, input.AskingPrice);
        }

        private static DecisionType DetermineDecision(int score)
        {
            return score switch
            {
                >= PonyUpThreshold => DecisionType.Buy,
                >= CautionThreshold => DecisionType.Consider,
                _ => DecisionType.Stop
            };
        }

        private static string DetermineRiskLevel(int riskScore)
        {
            return riskScore switch
            {
                <= 24 => "Low",
                <= 49 => "Moderate",
                <= 74 => "High",
                _ => "Severe"
            };
        }

        private static string DetermineConfidenceLevel(
            int confidenceScore)
        {
            return confidenceScore switch
            {
                >= 85 => "High",
                >= 65 => "Moderate",
                _ => "Low"
            };
        }

        private static string BuildDecisionSummary(
            DecisionType decision)
        {
            return decision switch
            {
                DecisionType.Buy => "Pony Up!",
                DecisionType.Consider => "Proceed With Caution",
                _ => "STOP! Walk Away"
            };
        }

        private static List<string> BuildStrengths(
            BuyDecisionInput input,
            IEnumerable<BuyScoreAdjustment> adjustments,
            decimal estimatedEquity)
        {
            var strengths = adjustments
                .Where(adjustment => adjustment.Adjustment > 0)
                .OrderByDescending(adjustment => adjustment.Adjustment)
                .Select(adjustment => adjustment.Explanation)
                .ToList();

            if (estimatedEquity > 0)
            {
                strengths.Add(
                    $"The estimated acquisition cost leaves approximately " +
                    $"{estimatedEquity:C0} in potential equity.");
            }

            return strengths;
        }

        private static List<string> BuildConcerns(
            BuyDecisionInput input,
            IEnumerable<BuyScoreAdjustment> adjustments,
            decimal estimatedEquity)
        {
            var concerns = adjustments
                .Where(adjustment => adjustment.Adjustment < 0)
                .OrderBy(adjustment => adjustment.Adjustment)
                .Select(adjustment => adjustment.Explanation)
                .ToList();

            if (estimatedEquity < 0)
            {
                concerns.Add(
                    $"The estimated acquisition cost exceeds market value by " +
                    $"{Math.Abs(estimatedEquity):C0}.");
            }

            if (input.EstimatedRepairCost > 0)
            {
                concerns.Add(
                    $"The purchase requires approximately " +
                    $"{input.EstimatedRepairCost:C0} in known repairs.");
            }

            return concerns.Distinct().ToList();
        }

        private static List<string> BuildNextSteps(
            BuyDecisionInput input,
            DecisionType decision,
            string riskLevel)
        {
            var nextSteps = new List<string>();

            if (decision == DecisionType.Stop)
            {
                nextSteps.Add(
                    "Do not proceed unless the price or verified condition changes materially.");

                nextSteps.Add(
                    "Compare this vehicle with cleaner alternatives before reconsidering.");
            }
            else
            {
                nextSteps.Add(
                    "Schedule an independent pre-purchase inspection.");

                nextSteps.Add(
                    "Verify the VIN, title, ownership history, accident history, and service records.");

                nextSteps.Add(
                    "Confirm the repair estimate with a qualified repair facility.");
            }

            if (riskLevel is "High" or "Severe")
            {
                nextSteps.Add(
                    "Do not exchange funds until all high-risk findings are independently verified.");
            }

            if (input.TitleStatus != TitleStatus.Clean)
            {
                nextSteps.Add(
                    "Confirm insurability, registration eligibility, and resale restrictions before purchase.");
            }

            return nextSteps.Distinct().ToList();
        }

        private static string BuildRiskExplanation(
            BuyDecisionInput input,
            int riskScore)
        {
            var primaryRisks = new List<string>();

            if (input.MechanicalCondition
                is MechanicalCondition.Poor
                or MechanicalCondition.Severe)
            {
                primaryRisks.Add("mechanical condition");
            }

            if (input.TitleStatus != TitleStatus.Clean)
            {
                primaryRisks.Add("title status");
            }

            if (input.AccidentHistory
                is AccidentHistory.Moderate
                or AccidentHistory.Major)
            {
                primaryRisks.Add("accident history");
            }

            if (input.EstimatedRepairCost > input.MarketValue * 0.20m)
            {
                primaryRisks.Add("repair exposure");
            }

            return primaryRisks.Count == 0
                ? $"The independent risk score is {riskScore}/100, with no major risk category dominating the analysis."
                : $"The independent risk score is {riskScore}/100. Primary risk areas: {string.Join(", ", primaryRisks)}.";
        }

        private static string BuildConfidenceExplanation(
            BuyDecisionInput input)
        {
            var missingData = new List<string>();

            if (string.IsNullOrWhiteSpace(input.Vin))
            {
                missingData.Add("VIN");
            }

            if (string.IsNullOrWhiteSpace(input.Trim))
            {
                missingData.Add("trim");
            }

            if (string.IsNullOrWhiteSpace(input.Engine))
            {
                missingData.Add("engine");
            }

            if (input.MechanicalCondition == MechanicalCondition.NotProvided)
            {
                missingData.Add("mechanical condition");
            }

            if (input.TitleStatus == TitleStatus.NotProvided)
            {
                missingData.Add("title status");
            }

            if (input.AccidentHistory == AccidentHistory.NotProvided)
            {
                missingData.Add("accident history");
            }

            return missingData.Count == 0
                ? "Confidence is supported by complete vehicle and purchase-condition data."
                : $"Confidence is limited by missing or unverified data: {string.Join(", ", missingData)}.";
        }

        private static string BuildReasoning(
            int decisionScore,
            DecisionType decision,
            IEnumerable<BuyScoreAdjustment> adjustments,
            decimal totalAcquisitionCost,
            decimal estimatedEquity)
        {
            var strongestPositive = adjustments
                .Where(adjustment => adjustment.Adjustment > 0)
                .OrderByDescending(adjustment => adjustment.Adjustment)
                .FirstOrDefault();

            var strongestNegative = adjustments
                .Where(adjustment => adjustment.Adjustment < 0)
                .OrderBy(adjustment => adjustment.Adjustment)
                .FirstOrDefault();

            var reasoning =
                $"The decision score is {decisionScore}/100, producing a " +
                $"{BuildDecisionSummary(decision)} recommendation. " +
                $"Estimated total acquisition cost is {totalAcquisitionCost:C0}, " +
                $"with estimated equity of {estimatedEquity:C0}.";

            if (strongestPositive is not null)
            {
                reasoning +=
                    $" The strongest positive factor is " +
                    $"{strongestPositive.Influencer.ToLowerInvariant()} " +
                    $"({FormatSignedNumber(strongestPositive.Adjustment)} points).";
            }

            if (strongestNegative is not null)
            {
                reasoning +=
                    $" The strongest concern is " +
                    $"{strongestNegative.Influencer.ToLowerInvariant()} " +
                    $"({FormatSignedNumber(strongestNegative.Adjustment)} points).";
            }

            return reasoning;
        }

        private static BuyScoreAdjustment CreateAdjustment(
            string influencer,
            int adjustment,
            int maximumAdjustment,
            string explanation)
        {
            return new BuyScoreAdjustment
            {
                Influencer = influencer,
                Adjustment = adjustment,
                MaximumAdjustment = maximumAdjustment,
                Explanation = explanation
            };
        }

        private static decimal RoundCurrency(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string FormatSignedNumber(int value)
        {
            return value > 0
                ? $"+{value}"
                : value.ToString();
        }

        private static string FormatEnum<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            var text = value.ToString();
            var characters = new List<char>();

            for (var index = 0; index < text.Length; index++)
            {
                if (index > 0
                    && char.IsUpper(text[index])
                    && !char.IsUpper(text[index - 1]))
                {
                    characters.Add(' ');
                }

                characters.Add(text[index]);
            }

            return new string(characters.ToArray());
        }
    }
}
