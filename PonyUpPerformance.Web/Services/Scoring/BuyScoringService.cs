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

        private const decimal MinimumBuyerNegotiationMargin = 0.10m;
        private const decimal MaximumBuyerNegotiationMargin = 0.15m;

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

            var totalAdjustment =
                adjustments.Sum(x => x.Adjustment);

            var score = Math.Clamp(
                BaseScore + totalAdjustment,
                0,
                100);

            var recommendation =
                DetermineRecommendation(score);

            var riskScore =
                CalculateRiskScore(input);

            var riskLevel =
                DetermineRiskLevel(riskScore);

            var confidenceScore =
                CalculateConfidenceScore(input);

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

            /*
             * PONYUP PRICING MATRIX
             *
             * Market Value
             *      ↓
             * Vehicle-specific adjustments
             *      ↓
             * Fair Asking Price
             *      ↓
             * Buyer-risk ceiling
             *      ↓
             * Maximum Recommended Price
             *      ↓
             * 10–15% Buyer Negotiation Margin
             *      ↓
             * Fair Purchase Price
             *      ↓
             * Suggested First Offer
             */

            var fairAskingPrice =
                CalculateFairAskingPrice(input);

            var maximumRecommendedPrice =
                CalculateMaximumRecommendedPrice(
                    fairAskingPrice,
                    riskScore);

            var buyerNegotiationMargin =
                CalculateBuyerNegotiationMargin(input);

            var fairPurchasePrice =
                CalculateFairPurchasePrice(
                    maximumRecommendedPrice,
                    buyerNegotiationMargin);

            var suggestedFirstOffer =
                CalculateSuggestedFirstOffer(
                    input,
                    fairPurchasePrice,
                    buyerNegotiationMargin);

            return new BuyDecisionResult
            {
                Score = score,

                Recommendation =
                    recommendation,

                ConfidenceScore =
                    confidenceScore,

                RiskLevel =
                    riskLevel,

                FinancialImpact =
                    BuildFinancialImpact(
                        input,
                        knownAcquisitionCost,
                        estimatedEquity),

                Reasoning =
                    BuildReasoning(
                        score,
                        recommendation,
                        adjustments,
                        input,
                        knownAcquisitionCost,
                        estimatedEquity,
                        fairAskingPrice,
                        maximumRecommendedPrice,
                        buyerNegotiationMargin),

                NextSteps =
                    BuildNextSteps(
                        input,
                        score,
                        riskLevel),

                FairAskingPrice =
                    RoundCurrency(
                        fairAskingPrice),

                MaximumRecommendedPrice =
                    RoundCurrency(
                        maximumRecommendedPrice),

                FairPurchasePrice =
                    RoundCurrency(
                        fairPurchasePrice),

                SuggestedFirstOffer =
                    RoundCurrency(
                        suggestedFirstOffer),

                BuyerNegotiationMarginPercent =
                    buyerNegotiationMargin.HasValue
                        ? Math.Round(
                            buyerNegotiationMargin.Value * 100m,
                            1,
                            MidpointRounding.AwayFromZero)
                        : null
            };
        }

        private static BuyScoreAdjustment ScoreMechanicalCondition(
            BuyDecisionInput input)
        {
            var adjustment = 0;

            var explanationParts =
                new List<string>();

            if (input.MechanicalCondition !=
                MechanicalCondition.NotProvided)
            {
                adjustment +=
                    input.MechanicalCondition switch
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

                adjustment +=
                    repairRatio switch
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
                string.Join(
                    " ",
                    explanationParts));
        }

        private static BuyScoreAdjustment ScoreAskingPrice(
            BuyDecisionInput input)
        {
            if (!input.AskingPrice.HasValue ||
                input.AskingPrice.Value <= 0)
            {
                return CreateAdjustment(
                    "Asking Price vs Fair Asking Price",
                    0,
                    AskingPriceMaximum,
                    "Asking price was not provided, so price position was not scored.");
            }

            var fairAskingPrice =
                CalculateFairAskingPrice(input);

            if (!fairAskingPrice.HasValue ||
                fairAskingPrice.Value <= 0)
            {
                return CreateAdjustment(
                    "Asking Price vs Fair Asking Price",
                    0,
                    AskingPriceMaximum,
                    "Market value was not available, so PonyUp could not calculate a fair asking price for comparison.");
            }

            var askingPrice =
                input.AskingPrice.Value;

            var priceDifferenceRatio =
                (fairAskingPrice.Value -
                 askingPrice) /
                fairAskingPrice.Value;

            var adjustment =
                priceDifferenceRatio switch
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
                      $"{priceDifferenceRatio:P0} below PonyUp's fair asking price."
                    : $"The asking price is " +
                      $"{Math.Abs(priceDifferenceRatio):P0} above PonyUp's fair asking price.";

            return CreateAdjustment(
                "Asking Price vs Fair Asking Price",
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

            var vehicleAge =
                CalculateVehicleAge(
                    input.Year.Value);

            var expectedMileage =
                vehicleAge * 12_000m;

            var mileageRatio =
                input.Mileage.Value /
                expectedMileage;

            var adjustment =
                mileageRatio switch
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
            var adjustment =
                input.TitleStatus switch
                {
                    TitleStatus.Clean => 8,
                    TitleStatus.Rebuilt => -4,
                    TitleStatus.Salvage => -7,
                    TitleStatus.Flood => -8,
                    _ => 0
                };

            var explanation =
                input.TitleStatus switch
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
            var adjustment =
                input.AccidentHistory switch
                {
                    AccidentHistory.None => 8,
                    AccidentHistory.Minor => 2,
                    AccidentHistory.Moderate => -3,
                    AccidentHistory.Major => -8,
                    _ => 0
                };

            var explanation =
                input.AccidentHistory switch
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
            var adjustment =
                input.IntendedUse switch
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
                else if (
                    input.MechanicalCondition ==
                    MechanicalCondition.Poor)
                {
                    adjustment -= 3;
                }
                else if (
                    input.MechanicalCondition ==
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
                        input.EstimatedRepairCost ??
                        0m;

                    if (input.AskingPrice.Value +
                        knownRepairCost <=
                        input.MarketValue.Value *
                        0.80m)
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
                input.IntendedUse ==
                IntendedUse.NotProvided
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

            risk +=
                input.MechanicalCondition switch
                {
                    MechanicalCondition.Excellent => 2,
                    MechanicalCondition.Good => 6,
                    MechanicalCondition.Fair => 14,
                    MechanicalCondition.Poor => 24,
                    MechanicalCondition.Severe => 32,
                    _ => 0
                };

            risk +=
                input.TitleStatus switch
                {
                    TitleStatus.Clean => 0,
                    TitleStatus.Rebuilt => 12,
                    TitleStatus.Salvage => 22,
                    TitleStatus.Flood => 28,
                    _ => 0
                };

            risk +=
                input.AccidentHistory switch
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

                    risk +=
                        repairRatio switch
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
                        input.EstimatedRepairCost ??
                        0m;

                    var totalCostRatio =
                        (input.AskingPrice.Value +
                         knownRepairCost) /
                        marketValue;

                    risk +=
                        totalCostRatio switch
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

            if (!string.IsNullOrWhiteSpace(
                    input.Vin))
            {
                confidence += 12;
            }

            if (input.Year.HasValue)
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Make))
            {
                confidence += 5;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Model))
            {
                confidence += 5;
            }

            if (input.Mileage.HasValue)
            {
                confidence += 7;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Trim))
            {
                confidence += 3;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Engine))
            {
                confidence += 4;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Transmission))
            {
                confidence += 4;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.Drivetrain))
            {
                confidence += 4;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.BodyStyle))
            {
                confidence += 3;
            }

            if (!string.IsNullOrWhiteSpace(
                    input.FuelType))
            {
                confidence += 3;
            }

            if (input.AskingPrice.HasValue)
            {
                confidence += 7;
            }

            if (input.MarketValue.HasValue)
            {
                confidence += 8;
            }

            if (input.EstimatedRepairCost.HasValue)
            {
                confidence += 4;
            }

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

        private static decimal? CalculateFairAskingPrice(
            BuyDecisionInput input)
        {
            if (!input.MarketValue.HasValue ||
                input.MarketValue.Value <= 0)
            {
                return null;
            }

            var marketValue =
                input.MarketValue.Value;

            /*
             * Fair Asking Price starts with market value.
             * We then adjust for the actual vehicle.
             */

            var conditionAdjustment =
                input.MechanicalCondition switch
                {
                    MechanicalCondition.Excellent => 0.04m,
                    MechanicalCondition.Good => 0.02m,
                    MechanicalCondition.Fair => -0.04m,
                    MechanicalCondition.Poor => -0.10m,
                    MechanicalCondition.Severe => -0.18m,
                    _ => 0m
                };

            var mileageAdjustment =
                CalculateMileageValueAdjustment(
                    input);

            var titleAdjustment =
                input.TitleStatus switch
                {
                    TitleStatus.Clean => 0m,
                    TitleStatus.Rebuilt => -0.12m,
                    TitleStatus.Salvage => -0.25m,
                    TitleStatus.Flood => -0.35m,
                    _ => 0m
                };

            var accidentAdjustment =
                input.AccidentHistory switch
                {
                    AccidentHistory.None => 0m,
                    AccidentHistory.Minor => -0.03m,
                    AccidentHistory.Moderate => -0.08m,
                    AccidentHistory.Major => -0.18m,
                    _ => 0m
                };

            var totalPercentageAdjustment =
                Math.Clamp(
                    conditionAdjustment +
                    mileageAdjustment +
                    titleAdjustment +
                    accidentAdjustment,
                    -0.70m,
                    0.12m);

            /*
             * Known repairs are real dollars,
             * so they are deducted directly.
             *
             * $0 repairs means $0 deduction.
             */

            var knownRepairCost =
                input.EstimatedRepairCost.HasValue &&
                input.EstimatedRepairCost.Value > 0
                    ? input.EstimatedRepairCost.Value
                    : 0m;

            var fairAskingPrice =
                (marketValue *
                 (1m +
                  totalPercentageAdjustment)) -
                knownRepairCost;

            return Math.Max(
                0m,
                fairAskingPrice);
        }

        private static decimal CalculateMileageValueAdjustment(
            BuyDecisionInput input)
        {
            var mileageRatio =
                CalculateMileageRatio(input);

            if (!mileageRatio.HasValue)
            {
                return 0m;
            }

            return mileageRatio.Value switch
            {
                <= 0.50m => 0.06m,
                <= 0.70m => 0.04m,
                <= 0.85m => 0.025m,
                <= 1.00m => 0.01m,
                <= 1.15m => 0m,
                <= 1.35m => -0.04m,
                <= 1.60m => -0.08m,
                _ => -0.12m
            };
        }

        private static decimal? CalculateMaximumRecommendedPrice(
            decimal? fairAskingPrice,
            int riskScore)
        {
            if (!fairAskingPrice.HasValue ||
                fairAskingPrice.Value <= 0)
            {
                return null;
            }

            /*
             * Fair Asking Price describes the car's
             * seller-side value.
             *
             * Maximum Recommended Price is the buyer's
             * absolute ceiling, so higher decision risk
             * creates additional buyer protection.
             */

            var buyerProtectionDiscount =
                riskScore switch
                {
                    <= 24 => 0m,
                    <= 49 => 0.02m,
                    <= 74 => 0.05m,
                    _ => 0.10m
                };

            return Math.Max(
                0m,
                fairAskingPrice.Value *
                (1m -
                 buyerProtectionDiscount));
        }

        private static decimal? CalculateBuyerNegotiationMargin(
            BuyDecisionInput input)
        {
            if (!input.MarketValue.HasValue ||
                input.MarketValue.Value <= 0)
            {
                return null;
            }

            /*
             * Mileage vs. expected mileage establishes
             * the base Buyer Negotiation Margin.
             *
             * Less mileage = smaller percentage.
             * More mileage = larger percentage.
             */

            var mileageRatio =
                CalculateMileageRatio(input);

            var margin =
                !mileageRatio.HasValue
                    ? 0.125m
                    : mileageRatio.Value switch
                    {
                        <= 0.70m => 0.10m,
                        <= 0.90m => 0.11m,
                        <= 1.10m => 0.12m,
                        <= 1.35m => 0.13m,
                        <= 1.60m => 0.14m,
                        _ => 0.15m
                    };

            /*
             * Other risk factors can push the margin
             * upward, but the final result is always
             * clamped between 10% and 15%.
             */

            margin +=
                input.MechanicalCondition switch
                {
                    MechanicalCondition.Excellent => -0.005m,
                    MechanicalCondition.Good => 0m,
                    MechanicalCondition.Fair => 0.005m,
                    MechanicalCondition.Poor => 0.010m,
                    MechanicalCondition.Severe => 0.015m,
                    _ => 0m
                };

            margin +=
                input.TitleStatus switch
                {
                    TitleStatus.Clean => 0m,
                    TitleStatus.Rebuilt => 0.005m,
                    TitleStatus.Salvage => 0.010m,
                    TitleStatus.Flood => 0.015m,
                    _ => 0m
                };

            margin +=
                input.AccidentHistory switch
                {
                    AccidentHistory.None => 0m,
                    AccidentHistory.Minor => 0.0025m,
                    AccidentHistory.Moderate => 0.005m,
                    AccidentHistory.Major => 0.010m,
                    _ => 0m
                };

            if (input.EstimatedRepairCost.HasValue &&
                input.EstimatedRepairCost.Value > 0)
            {
                var repairRatio =
                    input.EstimatedRepairCost.Value /
                    input.MarketValue.Value;

                margin +=
                    repairRatio switch
                    {
                        <= 0.05m => 0m,
                        <= 0.10m => 0.0025m,
                        <= 0.20m => 0.005m,
                        _ => 0.010m
                    };
            }

            return Math.Clamp(
                margin,
                MinimumBuyerNegotiationMargin,
                MaximumBuyerNegotiationMargin);
        }

        private static decimal? CalculateFairPurchasePrice(
            decimal? maximumRecommendedPrice,
            decimal? buyerNegotiationMargin)
        {
            if (!maximumRecommendedPrice.HasValue ||
                maximumRecommendedPrice.Value <= 0 ||
                !buyerNegotiationMargin.HasValue)
            {
                return null;
            }

            return Math.Max(
                0m,
                maximumRecommendedPrice.Value *
                (1m -
                 buyerNegotiationMargin.Value));
        }

        private static decimal? CalculateSuggestedFirstOffer(
            BuyDecisionInput input,
            decimal? fairPurchasePrice,
            decimal? buyerNegotiationMargin)
        {
            if (!fairPurchasePrice.HasValue ||
                fairPurchasePrice.Value <= 0 ||
                !buyerNegotiationMargin.HasValue)
            {
                return null;
            }

            /*
             * First offer room is also matrix-derived.
             * It scales with the same negotiation risk
             * instead of using a fixed dollar amount.
             */

            var openingOfferDiscount =
                Math.Clamp(
                    buyerNegotiationMargin.Value *
                    0.35m,
                    0.035m,
                    0.05m);

            var firstOffer =
                fairPurchasePrice.Value *
                (1m -
                 openingOfferDiscount);

            /*
             * PonyUp should never recommend opening
             * above the seller's actual asking price.
             */

            if (input.AskingPrice.HasValue &&
                input.AskingPrice.Value > 0)
            {
                firstOffer =
                    Math.Min(
                        firstOffer,
                        input.AskingPrice.Value);
            }

            return Math.Max(
                0m,
                firstOffer);
        }

        private static decimal? CalculateMileageRatio(
            BuyDecisionInput input)
        {
            if (!input.Year.HasValue ||
                !input.Mileage.HasValue)
            {
                return null;
            }

            var vehicleAge =
                CalculateVehicleAge(
                    input.Year.Value);

            var expectedMileage =
                vehicleAge * 12_000m;

            if (expectedMileage <= 0)
            {
                return null;
            }

            return
                input.Mileage.Value /
                expectedMileage;
        }

        private static int CalculateVehicleAge(
            int year)
        {
            return Math.Max(
                1,
                DateTime.UtcNow.Year -
                year);
        }

        private static string DetermineRecommendation(
            int score)
        {
            return score switch
            {
                >= PonyUpThreshold =>
                    "PONY UP",

                >= CautionThreshold =>
                    "PROCEED WITH CAUTION",

                _ =>
                    "STOP"
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
                        $"Based on the available market value, " +
                        $"estimated equity before any unknown repairs is " +
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
            var nextSteps =
                new List<string>();

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

            if (riskLevel is
                "High" or "Severe")
            {
                nextSteps.Add(
                    "Do not exchange funds until all high-risk findings are independently verified.");
            }

            if (input.TitleStatus !=
                    TitleStatus.NotProvided &&
                input.TitleStatus !=
                    TitleStatus.Clean)
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
            decimal? estimatedEquity,
            decimal? fairAskingPrice,
            decimal? maximumRecommendedPrice,
            decimal? buyerNegotiationMargin)
        {
            var strongestPositive =
                adjustments
                    .Where(x =>
                        x.Adjustment > 0)
                    .OrderByDescending(x =>
                        x.Adjustment)
                    .FirstOrDefault();

            var strongestNegative =
                adjustments
                    .Where(x =>
                        x.Adjustment < 0)
                    .OrderBy(x =>
                        x.Adjustment)
                    .FirstOrDefault();

            var reasoning =
                $"The decision score is {score}/100, producing a " +
                $"{recommendation} recommendation.";

            if (fairAskingPrice.HasValue)
            {
                reasoning +=
                    $" PonyUp estimates a fair asking price of " +
                    $"{fairAskingPrice.Value:C0} after adjusting the provided market value for condition, mileage, title, accident history, and known repairs.";
            }

            if (maximumRecommendedPrice.HasValue)
            {
                reasoning +=
                    $" The maximum recommended buyer price is " +
                    $"{maximumRecommendedPrice.Value:C0}.";
            }

            if (buyerNegotiationMargin.HasValue)
            {
                reasoning +=
                    $" The Buyer Negotiation Margin is " +
                    $"{buyerNegotiationMargin.Value:P1}.";
            }

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
                Influencer =
                    influencer,

                Adjustment =
                    adjustment,

                MaximumAdjustment =
                    maximumAdjustment,

                Explanation =
                    explanation
            };
        }

        private static decimal? RoundCurrency(
            decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return Math.Round(
                value.Value,
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
            var text =
                value.ToString();

            var characters =
                new List<char>();

            for (var index = 0;
                 index < text.Length;
                 index++)
            {
                if (index > 0 &&
                    char.IsUpper(
                        text[index]) &&
                    !char.IsUpper(
                        text[index - 1]))
                {
                    characters.Add(' ');
                }

                characters.Add(
                    text[index]);
            }

            return new string(
                characters.ToArray());
        }
    }
}
