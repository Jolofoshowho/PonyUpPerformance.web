using System;
using System.Collections.Generic;
using System.Linq;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring
{
    public class BuyScoringService : IBuyScoringService
    {
        private const int BaseScore = 50;

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

            var totalAdjustment = adjustments.Sum(x => x.Adjustment);

            var score = Math.Clamp(
                BaseScore + totalAdjustment,
                0,
                100);

            var recommendation = DetermineRecommendation(score);

            var riskScore = CalculateRiskScore(input);
            var riskLevel = DetermineRiskLevel(riskScore);

            var confidenceScore = CalculateConfidenceScore(input);

            decimal? knownAcquisitionCost = null;
            decimal? estimatedEquity = null;

            if (input.AskingPrice.HasValue)
            {
                knownAcquisitionCost =
                    input.AskingPrice.Value +
                    (input.EstimatedRepairCost ?? 0m);

                if (input.MarketValue.HasValue &&
                    input.MarketValue.Value > 0)
                {
                    estimatedEquity =
                        input.MarketValue.Value -
                        knownAcquisitionCost.Value;
                }
            }

            var maximumRecommendedPrice =
                CalculateMaximumRecommendedPrice(input);

            var fairPurchasePrice =
                CalculateFairPurchasePrice(
                    input,
                    maximumRecommendedPrice);

            var suggestedFirstOffer =
                CalculateSuggestedFirstOffer(
                    input,
                    fairPurchasePrice);

            return new BuyDecisionResult
            {
                Score = score,
                Recommendation = recommendation,
                ConfidenceScore = confidenceScore,
                RiskLevel = riskLevel,

                FinancialImpact = BuildFinancialImpact(
                    input,
                    knownAcquisitionCost,
                    estimatedEquity),

                Reasoning = BuildReasoning(
                    score,
                    recommendation,
                    adjustments,
                    input,
                    knownAcquisitionCost,
                    estimatedEquity),

                NextSteps = BuildNextSteps(
                    input,
                    score,
                    riskLevel),

                MaximumRecommendedPrice =
                    RoundCurrency(maximumRecommendedPrice),

                FairPurchasePrice =
                    RoundCurrency(fairPurchasePrice),

                SuggestedFirstOffer =
                    RoundCurrency(suggestedFirstOffer)
            };
        }

        private static BuyScoreAdjustment ScoreMechanicalCondition(
            BuyDecisionInput input)
        {
            var adjustment = 0;
            var explanationParts = new List<string>();

            if (input.MechanicalCondition !=
                MechanicalCondition.NotProvided)
            {
                adjustment += input.MechanicalCondition switch
                {
                    MechanicalCondition.Excellent => 8,
                    MechanicalCondition.Good => 5,
                    MechanicalCondition.Fair => 0,
                    MechanicalCondition.Poor => -6,
                    MechanicalCondition.Severe => -10,
                    _ => 0
                };

                explanationParts.Add(
                    $"Mechanical condition is " +
                    $"{FormatEnum(input.MechanicalCondition)}.");
            }
            else
            {
                explanationParts.Add(
                    "Mechanical condition was not provided.");
            }

            if (input.EstimatedRepairCost.HasValue &&
                input.MarketValue.HasValue &&
                input.MarketValue.Value > 0)
            {
                var repairRatio =
                    input.EstimatedRepairCost.Value /
                    input.MarketValue.Value;

                adjustment += repairRatio switch
                {
                    <= 0.02m => 4,
                    <= 0.05m => 2,
                    <= 0.10m => 0,
                    <= 0.20m => -4,
                    <= 0.35m => -8,
                    _ => -12
                };

                explanationParts.Add(
                    $"Known estimated repairs equal " +
                    $"{repairRatio:P0} of market value.");
            }
            else if (input.EstimatedRepairCost.HasValue)
            {
                explanationParts.Add(
                    "A repair cost was provided, but market value was unavailable for comparison.");
            }
            else
            {
                explanationParts.Add(
                    "Repair cost was not provided, so no repair-cost adjustment was applied.");
            }

            adjustment = Math.Clamp(
                adjustment,
                -MechanicalMaximum,
                MechanicalMaximum);

            return CreateAdjustment(
                "Mechanical Condition vs Needed Repairs",
                adjustment,
                MechanicalMaximum,
                string.Join(" ", explanationParts));
        }

        private static BuyScoreAdjustment ScoreAskingPrice(
            BuyDecisionInput input)
        {
            if (!input.AskingPrice.HasValue ||
                input.AskingPrice.Value <= 0 ||
                !input.MarketValue.HasValue ||
                input.MarketValue.Value <= 0)
            {
                return CreateAdjustment(
                    "Asking Price vs Market",
                    0,
                    AskingPriceMaximum,
                    "Asking price and market value were not both available, so price position was not scored.");
            }

            var marketValue = input.MarketValue.Value;
            var askingPrice = input.AskingPrice.Value;

            var priceDifferenceRatio =
                (marketValue - askingPrice) /
                marketValue;

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

            var explanation =
                priceDifferenceRatio >= 0
                    ? $"The asking price is " +
                      $"{priceDifferenceRatio:P0} below estimated market value."
                    : $"The asking price is " +
                      $"{Math.Abs(priceDifferenceRatio):P0} above estimated market value.";

            return CreateAdjustment(
                "Asking Price vs Market",
                adjustment,
                AskingPriceMaximum,
                explanation);
        }

        private static BuyScoreAdjustment ScoreMileageForAge(
            BuyDecisionInput input)
        {
            if (!input.Year.HasValue ||
                !input.Mileage.HasValue)
            {
                return CreateAdjustment(
                    "Mileage vs Age",
                    0,
                    MileageMaximum,
                    "Mileage was not provided, so mileage versus age was not scored.");
            }

            var currentYear = DateTime.UtcNow.Year;

            var vehicleAge = Math.Max(
                1,
                currentYear - input.Year.Value);

            var expectedMileage =
                vehicleAge * 12_000m;

            var mileageRatio =
                input.Mileage.Value /
                expectedMileage;

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

            var averageAnnualMileage =
                input.Mileage.Value /
                (decimal)vehicleAge;

            var explanation =
                $"The vehicle averages approximately " +
                $"{Math.Round(averageAnnualMileage):N0} miles per year " +
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
                    "A rebuilt title can reduce resale value and may limit insurance or financing options.",

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

            if (input.IntendedUse is
                IntendedUse.DailyDriver
                or IntendedUse.WorkVehicle
                or IntendedUse.FamilyVehicle)
            {
                if (input.MechanicalCondition ==
                    MechanicalCondition.Excellent)
                {
                    adjustment += 2;
                }
                else if (input.MechanicalCondition ==
                         MechanicalCondition.Poor)
                {
                    adjustment -= 3;
                }
                else if (input.MechanicalCondition ==
                         MechanicalCondition.Severe)
                {
                    adjustment -= 4;
                }
            }

            if (input.IntendedUse is
                IntendedUse.ProjectVehicle
                or IntendedUse.PerformanceBuild)
            {
                if (input.MarketValue.HasValue &&
                    input.MarketValue.Value > 0 &&
                    input.AskingPrice.HasValue)
                {
                    var knownRepairCost =
                        input.EstimatedRepairCost ?? 0m;

                    if (input.AskingPrice.Value +
                        knownRepairCost <=
                        input.MarketValue.Value * 0.80m)
                    {
                        adjustment += 2;
                    }
                }
            }

            adjustment = Math.Clamp(
                adjustment,
                -IntendedUseMaximum,
                IntendedUseMaximum);

            var explanation =
                input.IntendedUse == IntendedUse.NotProvided
                    ? "Intended use was not provided."
                    : $"The vehicle is being evaluated as a " +
                      $"{FormatEnum(input.IntendedUse)}.";

            return CreateAdjustment(
                "Intended Use",
                adjustment,
                IntendedUseMaximum,
                explanation);
        }

        private static int CalculateRiskScore(
            BuyDecisionInput input)
        {
            var risk = 0;

            risk += input.MechanicalCondition switch
            {
                MechanicalCondition.Excellent => 2,
                MechanicalCondition.Good => 6,
                MechanicalCondition.Fair => 14,
                MechanicalCondition.Poor => 24,
                MechanicalCondition.Severe => 32,
                _ => 0
            };

            risk += input.TitleStatus switch
            {
                TitleStatus.Clean => 0,
                TitleStatus.Rebuilt => 12,
                TitleStatus.Salvage => 22,
                TitleStatus.Flood => 28,
                _ => 0
            };

            risk += input.AccidentHistory switch
            {
                AccidentHistory.None => 0,
                AccidentHistory.Minor => 5,
                AccidentHistory.Moderate => 13,
                AccidentHistory.Major => 22,
                _ => 0
            };

            if (input.MarketValue.HasValue &&
                input.MarketValue.Value > 0)
            {
                var marketValue =
                    input.MarketValue.Value;

                if (input.EstimatedRepairCost.HasValue)
                {
                    var repairRatio =
                        input.EstimatedRepairCost.Value /
                        marketValue;

                    risk += repairRatio switch
                    {
                        <= 0.05m => 0,
                        <= 0.10m => 4,
                        <= 0.20m => 9,
                        <= 0.35m => 14,
                        _ => 18
                    };
                }

                if (input.AskingPrice.HasValue)
                {
                    var knownRepairCost =
                        input.EstimatedRepairCost ?? 0m;

                    var totalCostRatio =
                        (input.AskingPrice.Value +
                         knownRepairCost) /
                        marketValue;

                    risk += totalCostRatio switch
                    {
                        <= 0.80m => 0,
                        <= 0.95m => 3,
                        <= 1.05m => 7,
                        <= 1.20m => 12,
                        _ => 16
                    };
                }
            }

            return Math.Clamp(
                risk,
                0,
                100);
        }

        private static int CalculateConfidenceScore(
            BuyDecisionInput input)
        {
            var confidence = 35;

            if (!string.IsNullOrWhiteSpace(input.Vin))
                confidence += 12;

            if (input.Year.HasValue)
                confidence += 5;

            if (!string.IsNullOrWhiteSpace(input.Make))
                confidence += 5;

            if (!string.IsNullOrWhiteSpace(input.Model))
                confidence += 5;

            if (input.Mileage.HasValue)
                confidence += 7;

            if (!string.IsNullOrWhiteSpace(input.Trim))
                confidence += 3;

            if (!string.IsNullOrWhiteSpace(input.Engine))
                confidence += 4;

            if (!string.IsNullOrWhiteSpace(input.Transmission))
                confidence += 4;

            if (!string.IsNullOrWhiteSpace(input.Drivetrain))
                confidence += 4;

            if (!string.IsNullOrWhiteSpace(input.BodyStyle))
                confidence += 3;

            if (!string.IsNullOrWhiteSpace(input.FuelType))
                confidence += 3;

            if (input.AskingPrice.HasValue)
                confidence += 7;

            if (input.MarketValue.HasValue)
                confidence += 8;

            if (input.EstimatedRepairCost.HasValue)
                confidence += 4;

            if (input.MechanicalCondition !=
                MechanicalCondition.NotProvided)
            {
                confidence += 5;
            }

            if (input.TitleStatus !=
                TitleStatus.NotProvided)
            {
                confidence += 5;
            }

            if (input.AccidentHistory !=
                AccidentHistory.NotProvided)
            {
                confidence += 4;
            }

            if (input.IntendedUse !=
                IntendedUse.NotProvided)
            {
                confidence += 4;
            }

            return Math.Clamp(
                confidence,
                0,
                100);
        }

        private static decimal CalculateMaximumRecommendedPrice(
            BuyDecisionInput input)
        {
            if (!input.MarketValue.HasValue ||
                input.MarketValue.Value <= 0)
            {
                return 0m;
            }

            var marketValue =
                input.MarketValue.Value;

            var knownRepairCost =
                input.EstimatedRepairCost ?? 0m;

            var titleReserve = input.TitleStatus switch
            {
                TitleStatus.Clean => 0m,
                TitleStatus.Rebuilt => marketValue * 0.12m,
                TitleStatus.Salvage => marketValue * 0.25m,
                TitleStatus.Flood => marketValue * 0.35m,
                _ => 0m
            };

            var accidentReserve =
                input.AccidentHistory switch
                {
                    AccidentHistory.None => 0m,
                    AccidentHistory.Minor => marketValue * 0.03m,
                    AccidentHistory.Moderate => marketValue * 0.08m,
                    AccidentHistory.Major => marketValue * 0.18m,
                    _ => 0m
                };

            var repairReserve =
                input.EstimatedRepairCost.HasValue
                    ? Math.Max(
                        500m,
                        knownRepairCost * 0.20m)
                    : 0m;

            return Math.Max(
                0m,
                marketValue
                - knownRepairCost
                - repairReserve
                - titleReserve
                - accidentReserve);
        }

        private static decimal CalculateFairPurchasePrice(
            BuyDecisionInput input,
            decimal maximumRecommendedPrice)
        {
            if (!input.MarketValue.HasValue ||
                input.MarketValue.Value <= 0)
            {
                return 0m;
            }

            var marketValue =
                input.MarketValue.Value;

            var knownRepairCost =
                input.EstimatedRepairCost ?? 0m;

            var negotiationReserve =
                Math.Max(
                    500m,
                    marketValue * 0.05m);

            return Math.Max(
                0m,
                Math.Min(
                    maximumRecommendedPrice,
                    marketValue
                    - knownRepairCost
                    - negotiationReserve));
        }

        private static decimal CalculateSuggestedFirstOffer(
            BuyDecisionInput input,
            decimal fairPurchasePrice)
        {
            if (fairPurchasePrice <= 0)
            {
                return 0m;
            }

            var openingDiscount =
                Math.Max(
                    500m,
                    fairPurchasePrice * 0.08m);

            var firstOffer =
                Math.Max(
                    0m,
                    fairPurchasePrice
                    - openingDiscount);

            if (input.AskingPrice.HasValue &&
                input.AskingPrice.Value > 0)
            {
                return Math.Min(
                    firstOffer,
                    input.AskingPrice.Value);
            }

            return firstOffer;
        }

        private static string DetermineRecommendation(
            int score)
        {
            return score switch
            {
                >= PonyUpThreshold => "PONY UP",
                >= CautionThreshold => "PROCEED WITH CAUTION",
                _ => "STOP"
            };
        }

        private static string DetermineRiskLevel(
            int riskScore)
        {
            return riskScore switch
            {
                <= 24 => "Low",
                <= 49 => "Moderate",
                <= 74 => "High",
                _ => "Severe"
            };
        }

        private static string BuildFinancialImpact(
            BuyDecisionInput input,
            decimal? knownAcquisitionCost,
            decimal? estimatedEquity)
        {
            if (!input.AskingPrice.HasValue)
            {
                return
                    "Asking price was not available, so acquisition cost could not be calculated.";
            }

            if (!knownAcquisitionCost.HasValue)
            {
                return
                    "Known acquisition cost could not be calculated.";
            }

            if (!input.MarketValue.HasValue)
            {
                return
                    $"Known acquisition cost is approximately " +
                    $"{knownAcquisitionCost.Value:C0}. " +
                    $"Market value was not available for an equity comparison.";
            }

            if (!input.EstimatedRepairCost.HasValue)
            {
                if (estimatedEquity.HasValue)
                {
                    return
                        $"Purchase price is approximately " +
                        $"{input.AskingPrice.Value:C0}. " +
                        $"Based on the available market value, estimated equity before any unknown repairs is " +
                        $"{estimatedEquity.Value:C0}.";
                }

                return
                    $"Purchase price is approximately " +
                    $"{input.AskingPrice.Value:C0}. " +
                    $"Repair cost was not provided.";
            }

            if (!estimatedEquity.HasValue)
            {
                return
                    $"Known acquisition cost is approximately " +
                    $"{knownAcquisitionCost.Value:C0}.";
            }

            if (estimatedEquity.Value > 0)
            {
                return
                    $"Estimated acquisition cost is " +
                    $"{knownAcquisitionCost.Value:C0}, leaving approximately " +
                    $"{estimatedEquity.Value:C0} in potential equity.";
            }

            if (estimatedEquity.Value < 0)
            {
                return
                    $"Estimated acquisition cost is " +
                    $"{knownAcquisitionCost.Value:C0}, which exceeds market value " +
                    $"by approximately {Math.Abs(estimatedEquity.Value):C0}.";
            }

            return
                $"Estimated acquisition cost is " +
                $"{knownAcquisitionCost.Value:C0}, approximately equal to " +
                $"the estimated market value.";
        }

        private static List<string> BuildNextSteps(
            BuyDecisionInput input,
            int score,
            string riskLevel)
        {
            var nextSteps = new List<string>();

            if (score < CautionThreshold)
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

                if (input.EstimatedRepairCost.HasValue &&
                    input.EstimatedRepairCost.Value > 0)
                {
                    nextSteps.Add(
                        "Confirm the repair estimate with a qualified repair facility.");
                }
            }

            if (riskLevel is "High" or "Severe")
            {
                nextSteps.Add(
                    "Do not exchange funds until all high-risk findings are independently verified.");
            }

            if (input.TitleStatus != TitleStatus.NotProvided &&
                input.TitleStatus != TitleStatus.Clean)
            {
                nextSteps.Add(
                    "Confirm insurability, registration eligibility, and resale restrictions before purchase.");
            }

            return nextSteps
                .Distinct()
                .ToList();
        }

        private static string BuildReasoning(
            int score,
            string recommendation,
            IEnumerable<BuyScoreAdjustment> adjustments,
            BuyDecisionInput input,
            decimal? knownAcquisitionCost,
            decimal? estimatedEquity)
        {
            var strongestPositive = adjustments
                .Where(x => x.Adjustment > 0)
                .OrderByDescending(x => x.Adjustment)
                .FirstOrDefault();

            var strongestNegative = adjustments
                .Where(x => x.Adjustment < 0)
                .OrderBy(x => x.Adjustment)
                .FirstOrDefault();

            var reasoning =
                $"The decision score is {score}/100, producing a " +
                $"{recommendation} recommendation.";

            if (knownAcquisitionCost.HasValue)
            {
                reasoning +=
                    $" Known acquisition cost is approximately " +
                    $"{knownAcquisitionCost.Value:C0}.";
            }

            if (estimatedEquity.HasValue)
            {
                if (input.EstimatedRepairCost.HasValue)
                {
                    reasoning +=
                        $" Estimated equity is " +
                        $"{estimatedEquity.Value:C0}.";
                }
                else
                {
                    reasoning +=
                        $" Estimated equity before unknown repair costs is " +
                        $"{estimatedEquity.Value:C0}.";
                }
            }

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

        private static decimal RoundCurrency(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string FormatSignedNumber(
            int value)
        {
            return value > 0
                ? $"+{value}"
                : value.ToString();
        }

        private static string FormatEnum<TEnum>(
            TEnum value)
            where TEnum : struct, Enum
        {
            var text = value.ToString();
            var characters = new List<char>();

            for (var index = 0;
                 index < text.Length;
                 index++)
            {
                if (index > 0 &&
                    char.IsUpper(text[index]) &&
                    !char.IsUpper(text[index - 1]))
                {
                    characters.Add(' ');
                }

                characters.Add(text[index]);
            }

            return new string(
                characters.ToArray());
        }
    }
}
