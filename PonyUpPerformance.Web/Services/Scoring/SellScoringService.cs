using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class SellScoringService : ISellScoringService
{
    private const int BaseScore = 50;

    private const int SalePositionMaximum = 15;
    private const int RepairEconomicsMaximum = 12;
    private const int MechanicalMaximum = 8;
    private const int MileageMaximum = 6;
    private const int TitleMaximum = 5;
    private const int AccidentMaximum = 4;

    public SellDecisionResult Analyze(
        SellDecisionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        SellFactorScore salePosition =
            ScoreSalePosition(input);

        SellFactorScore repairEconomics =
            ScoreRepairEconomics(input);

        SellFactorScore mechanical =
            ScoreMechanicalCondition(input);

        SellFactorScore mileage =
            ScoreMileageForAge(input);

        SellFactorScore title =
            ScoreTitleStatus(input);

        SellFactorScore accident =
            ScoreAccidentHistory(input);

        var factors = new[]
        {
            salePosition,
            repairEconomics,
            mechanical,
            mileage,
            title,
            accident
        };

        int totalAdjustment =
            factors.Sum(x => x.Adjustment);

        int score =
            Math.Clamp(
                BaseScore + totalAdjustment,
                0,
                100);

        decimal? fairAskingPrice =
            CalculateFairAskingPrice(input);

        decimal? currentOfferGap = null;

        if (fairAskingPrice.HasValue &&
            input.ExpectedSalePrice.HasValue)
        {
            currentOfferGap =
                fairAskingPrice.Value -
                input.ExpectedSalePrice.Value;
        }

        decimal? repairBreakEvenSalePrice = null;

        if (input.ExpectedSalePrice.HasValue &&
            input.EstimatedRepairCost.HasValue)
        {
            repairBreakEvenSalePrice =
                input.ExpectedSalePrice.Value +
                input.EstimatedRepairCost.Value;
        }

        decimal? repairCostToMarketPercent = null;

        if (input.EstimatedRepairCost.HasValue &&
            input.MarketValue.HasValue &&
            input.MarketValue.Value > 0)
        {
            repairCostToMarketPercent =
                input.EstimatedRepairCost.Value /
                input.MarketValue.Value *
                100m;
        }

        int confidenceScore =
            CalculateConfidenceScore(input);

        bool repairMayPay =
            RepairMayPay(input);

        string recommendation =
            DetermineRecommendation(
                input,
                score,
                fairAskingPrice,
                repairMayPay);

        int riskScore =
            CalculateRiskScore(
                input,
                fairAskingPrice,
                confidenceScore);

        return new SellDecisionResult
        {
            Score = score,

            Recommendation =
                recommendation,

            ConfidenceScore =
                confidenceScore,

            RiskLevel =
                DetermineRiskLevel(riskScore),

            FairAskingPrice =
                RoundCurrency(fairAskingPrice),

            CurrentOfferGap =
                RoundCurrency(currentOfferGap),

            RepairBreakEvenSalePrice =
                RoundCurrency(
                    repairBreakEvenSalePrice),

            RepairCostToMarketPercent =
                repairCostToMarketPercent.HasValue
                    ? Math.Round(
                        repairCostToMarketPercent.Value,
                        1,
                        MidpointRounding.AwayFromZero)
                    : null,

            FinancialImpact =
                BuildFinancialImpact(
                    input,
                    fairAskingPrice,
                    currentOfferGap,
                    repairBreakEvenSalePrice),

            Reasoning =
                BuildReasoning(
                    factors,
                    recommendation),

            NextSteps =
                BuildNextSteps(
                    input,
                    recommendation,
                    repairMayPay)
        };
    }

    private static SellFactorScore ScoreSalePosition(
        SellDecisionInput input)
    {
        if (!input.ExpectedSalePrice.HasValue ||
            !input.MarketValue.HasValue ||
            input.MarketValue.Value <= 0)
        {
            return new SellFactorScore(
                "Sale Price vs Market",
                0,
                "Sale price and market value were not both provided.");
        }

        decimal ratio =
            input.ExpectedSalePrice.Value /
            input.MarketValue.Value;

        int adjustment =
            ratio switch
            {
                >= 1.05m => 15,
                >= 0.98m => 12,
                >= 0.92m => 8,
                >= 0.85m => 4,
                >= 0.75m => -4,
                >= 0.65m => -9,
                _ => -15
            };

        return new SellFactorScore(
            "Sale Price vs Market",
            adjustment,
            $"The current offer or expected as-is sale price is approximately {ratio:P0} of estimated market value.");
    }

    private static SellFactorScore ScoreRepairEconomics(
        SellDecisionInput input)
    {
        if (!input.EstimatedRepairCost.HasValue)
        {
            return new SellFactorScore(
                "Repair Before Sale Economics",
                0,
                "No pre-sale repair cost was provided.");
        }

        if (input.EstimatedRepairCost.Value <= 0)
        {
            return new SellFactorScore(
                "Repair Before Sale Economics",
                6,
                "No pre-sale repair expense is currently expected.");
        }

        if (!input.MarketValue.HasValue ||
            input.MarketValue.Value <= 0)
        {
            return new SellFactorScore(
                "Repair Before Sale Economics",
                0,
                "Repair cost was provided, but market value is unavailable for comparison.");
        }

        decimal repairCost =
            input.EstimatedRepairCost.Value;

        decimal marketValue =
            input.MarketValue.Value;

        decimal repairRatio =
            repairCost / marketValue;

        int adjustment;

        if (input.ExpectedSalePrice.HasValue)
        {
            decimal marketHeadroom =
                Math.Max(
                    0m,
                    marketValue -
                    input.ExpectedSalePrice.Value);

            if (marketHeadroom >= repairCost * 1.50m)
            {
                adjustment = 12;
            }
            else if (marketHeadroom >= repairCost)
            {
                adjustment = 7;
            }
            else
            {
                adjustment =
                    repairRatio switch
                    {
                        <= 0.03m => 3,
                        <= 0.08m => 0,
                        <= 0.15m => -6,
                        _ => -12
                    };
            }
        }
        else
        {
            adjustment =
                repairRatio switch
                {
                    <= 0.03m => 3,
                    <= 0.08m => 0,
                    <= 0.15m => -6,
                    _ => -12
                };
        }

        adjustment =
            Math.Clamp(
                adjustment,
                -RepairEconomicsMaximum,
                RepairEconomicsMaximum);

        return new SellFactorScore(
            "Repair Before Sale Economics",
            adjustment,
            $"Estimated repairs equal approximately {repairRatio:P0} of market value.");
    }

    private static SellFactorScore ScoreMechanicalCondition(
        SellDecisionInput input)
    {
        int adjustment =
            input.MechanicalCondition switch
            {
                MechanicalCondition.Excellent => 8,
                MechanicalCondition.Good => 4,
                MechanicalCondition.Fair => 0,
                MechanicalCondition.Poor => -5,
                MechanicalCondition.Severe => -8,
                _ => 0
            };

        string explanation =
            input.MechanicalCondition ==
            MechanicalCondition.NotProvided
                ? "Mechanical condition was not provided."
                : $"Mechanical condition is {input.MechanicalCondition}.";

        return new SellFactorScore(
            "Mechanical Condition",
            adjustment,
            explanation);
    }

    private static SellFactorScore ScoreMileageForAge(
        SellDecisionInput input)
    {
        if (!input.Year.HasValue ||
            !input.Mileage.HasValue)
        {
            return new SellFactorScore(
                "Mileage vs Age",
                0,
                "Year and mileage were not both provided.");
        }

        int age =
            Math.Max(
                1,
                DateTime.UtcNow.Year -
                input.Year.Value);

        decimal expectedMileage =
            age * 12_000m;

        decimal ratio =
            input.Mileage.Value /
            expectedMileage;

        int adjustment =
            ratio switch
            {
                <= 0.60m => 6,
                <= 0.80m => 4,
                <= 1.00m => 2,
                <= 1.20m => 0,
                <= 1.40m => -2,
                <= 1.70m => -4,
                _ => -6
            };

        return new SellFactorScore(
            "Mileage vs Age",
            adjustment,
            $"Mileage is approximately {ratio:P0} of the 12,000-mile-per-year benchmark.");
    }

    private static SellFactorScore ScoreTitleStatus(
        SellDecisionInput input)
    {
        int adjustment =
            input.TitleStatus switch
            {
                TitleStatus.Clean => 5,
                TitleStatus.Rebuilt => -2,
                TitleStatus.Salvage => -4,
                TitleStatus.Flood => -5,
                _ => 0
            };

        string explanation =
            input.TitleStatus ==
            TitleStatus.NotProvided
                ? "Title status was not provided."
                : $"Title status is {input.TitleStatus}.";

        return new SellFactorScore(
            "Title Status",
            adjustment,
            explanation);
    }

    private static SellFactorScore ScoreAccidentHistory(
        SellDecisionInput input)
    {
        int adjustment =
            input.AccidentHistory switch
            {
                AccidentHistory.None => 4,
                AccidentHistory.Minor => 1,
                AccidentHistory.Moderate => -2,
                AccidentHistory.Major => -4,
                _ => 0
            };

        string explanation =
            input.AccidentHistory ==
            AccidentHistory.NotProvided
                ? "Accident history was not provided."
                : $"Accident history is {input.AccidentHistory}.";

        return new SellFactorScore(
            "Accident History",
            adjustment,
            explanation);
    }

    private static decimal? CalculateFairAskingPrice(
        SellDecisionInput input)
    {
        if (!input.MarketValue.HasValue ||
            input.MarketValue.Value <= 0)
        {
            return null;
        }

        decimal adjustment = 0m;

        adjustment +=
            input.MechanicalCondition switch
            {
                MechanicalCondition.Excellent => 0.03m,
                MechanicalCondition.Good => 0.01m,
                MechanicalCondition.Fair => 0m,
                MechanicalCondition.Poor => -0.08m,
                MechanicalCondition.Severe => -0.15m,
                _ => 0m
            };

        adjustment +=
            input.TitleStatus switch
            {
                TitleStatus.Clean => 0m,
                TitleStatus.Rebuilt => -0.08m,
                TitleStatus.Salvage => -0.18m,
                TitleStatus.Flood => -0.25m,
                _ => 0m
            };

        adjustment +=
            input.AccidentHistory switch
            {
                AccidentHistory.None => 0m,
                AccidentHistory.Minor => -0.02m,
                AccidentHistory.Moderate => -0.06m,
                AccidentHistory.Major => -0.12m,
                _ => 0m
            };

        if (input.Year.HasValue &&
            input.Mileage.HasValue)
        {
            int age =
                Math.Max(
                    1,
                    DateTime.UtcNow.Year -
                    input.Year.Value);

            decimal expectedMileage =
                age * 12_000m;

            decimal mileageRatio =
                input.Mileage.Value /
                expectedMileage;

            adjustment +=
                mileageRatio switch
                {
                    <= 0.70m => 0.03m,
                    <= 1.00m => 0.01m,
                    <= 1.20m => 0m,
                    <= 1.40m => -0.03m,
                    <= 1.70m => -0.06m,
                    _ => -0.10m
                };
        }

        adjustment =
            Math.Clamp(
                adjustment,
                -0.45m,
                0.08m);

        return input.MarketValue.Value *
               (1m + adjustment);
    }

    private static bool RepairMayPay(
        SellDecisionInput input)
    {
        if (!input.EstimatedRepairCost.HasValue ||
            input.EstimatedRepairCost.Value <= 0 ||
            !input.ExpectedSalePrice.HasValue ||
            !input.MarketValue.HasValue ||
            input.MarketValue.Value <= 0)
        {
            return false;
        }

        decimal potentialHeadroom =
            input.MarketValue.Value -
            input.ExpectedSalePrice.Value;

        return potentialHeadroom >=
               input.EstimatedRepairCost.Value *
               1.15m;
    }

    private static string DetermineRecommendation(
        SellDecisionInput input,
        int score,
        decimal? fairAskingPrice,
        bool repairMayPay)
    {
        if (input.ExpectedSalePrice.HasValue &&
            fairAskingPrice.HasValue &&
            fairAskingPrice.Value > 0)
        {
            decimal offerRatio =
                input.ExpectedSalePrice.Value /
                fairAskingPrice.Value;

            if (offerRatio >= 0.95m &&
                score >= 55)
            {
                return "SELL NOW";
            }
        }

        if (repairMayPay)
        {
            return "REPAIR FIRST";
        }

        if (score >= 70)
        {
            return "SELL NOW";
        }

        return "HOLD / REWORK";
    }

    private static int CalculateConfidenceScore(
        SellDecisionInput input)
    {
        int confidence = 0;

        if (!string.IsNullOrWhiteSpace(input.Vin))
            confidence += 8;

        if (input.Year.HasValue)
            confidence += 5;

        if (!string.IsNullOrWhiteSpace(input.Make))
            confidence += 5;

        if (!string.IsNullOrWhiteSpace(input.Model))
            confidence += 5;

        if (!string.IsNullOrWhiteSpace(input.Trim))
            confidence += 2;

        if (input.Mileage.HasValue)
            confidence += 10;

        if (input.ExpectedSalePrice.HasValue)
            confidence += 18;

        if (input.MarketValue.HasValue)
            confidence += 18;

        if (input.EstimatedRepairCost.HasValue)
            confidence += 10;

        if (input.MechanicalCondition !=
            MechanicalCondition.NotProvided)
        {
            confidence += 7;
        }

        if (input.TitleStatus !=
            TitleStatus.NotProvided)
        {
            confidence += 6;
        }

        if (input.AccidentHistory !=
            AccidentHistory.NotProvided)
        {
            confidence += 6;
        }

        return Math.Clamp(
            confidence,
            0,
            100);
    }

    private static int CalculateRiskScore(
        SellDecisionInput input,
        decimal? fairAskingPrice,
        int confidenceScore)
    {
        int risk = 0;

        if (input.ExpectedSalePrice.HasValue &&
            fairAskingPrice.HasValue &&
            fairAskingPrice.Value > 0)
        {
            decimal ratio =
                input.ExpectedSalePrice.Value /
                fairAskingPrice.Value;

            risk +=
                ratio switch
                {
                    >= 0.95m => 0,
                    >= 0.85m => 10,
                    >= 0.75m => 20,
                    _ => 30
                };
        }

        if (input.EstimatedRepairCost.HasValue &&
            input.MarketValue.HasValue &&
            input.MarketValue.Value > 0)
        {
            decimal ratio =
                input.EstimatedRepairCost.Value /
                input.MarketValue.Value;

            risk +=
                ratio switch
                {
                    <= 0.03m => 2,
                    <= 0.08m => 8,
                    <= 0.15m => 15,
                    _ => 25
                };
        }

        risk +=
            input.MechanicalCondition switch
            {
                MechanicalCondition.Poor => 10,
                MechanicalCondition.Severe => 18,
                _ => 0
            };

        risk +=
            input.TitleStatus switch
            {
                TitleStatus.Rebuilt => 8,
                TitleStatus.Salvage => 15,
                TitleStatus.Flood => 20,
                _ => 0
            };

        risk +=
            input.AccidentHistory switch
            {
                AccidentHistory.Moderate => 8,
                AccidentHistory.Major => 15,
                _ => 0
            };

        if (confidenceScore < 40)
        {
            risk += 15;
        }
        else if (confidenceScore < 65)
        {
            risk += 8;
        }

        return Math.Clamp(
            risk,
            0,
            100);
    }

    private static string DetermineRiskLevel(
        int riskScore)
    {
        return riskScore switch
        {
            <= 25 => "LOW",
            <= 50 => "MODERATE",
            _ => "HIGH"
        };
    }

    private static string BuildFinancialImpact(
        SellDecisionInput input,
        decimal? fairAskingPrice,
        decimal? currentOfferGap,
        decimal? repairBreakEvenSalePrice)
    {
        var parts = new List<string>();

        if (input.ExpectedSalePrice.HasValue &&
            fairAskingPrice.HasValue &&
            currentOfferGap.HasValue)
        {
            if (currentOfferGap.Value > 0)
            {
                parts.Add(
                    $"The current offer or expected as-is sale price is approximately {currentOfferGap.Value:C0} below PonyUp's fair asking price.");
            }
            else if (currentOfferGap.Value < 0)
            {
                parts.Add(
                    $"The current offer or expected as-is sale price is approximately {Math.Abs(currentOfferGap.Value):C0} above PonyUp's fair asking price.");
            }
            else
            {
                parts.Add(
                    "The current offer is essentially at PonyUp's fair asking price.");
            }
        }

        if (input.EstimatedRepairCost.HasValue &&
            input.EstimatedRepairCost.Value > 0 &&
            repairBreakEvenSalePrice.HasValue)
        {
            parts.Add(
                $"If you repair before selling, the post-repair sale price must exceed approximately {repairBreakEvenSalePrice.Value:C0} just to outperform selling at the current as-is price.");
        }

        if (parts.Count == 0)
        {
            return
                "Add the expected sale price and estimated market value for a stronger financial comparison.";
        }

        return string.Join(
            " ",
            parts);
    }

    private static string BuildReasoning(
        IEnumerable<SellFactorScore> factors,
        string recommendation)
    {
        string factorText =
            string.Join(
                " ",
                factors.Select(
                    x =>
                        $"{x.Name}: {x.Explanation}"));

        return
            $"{recommendation}. {factorText}";
    }

    private static List<string> BuildNextSteps(
        SellDecisionInput input,
        string recommendation,
        bool repairMayPay)
    {
        var steps = new List<string>();

        if (!input.MarketValue.HasValue)
        {
            steps.Add(
                "Establish a realistic market value using comparable vehicles before accepting an offer.");
        }

        if (!input.ExpectedSalePrice.HasValue)
        {
            steps.Add(
                "Get at least one real purchase offer or establish an expected as-is selling price.");
        }

        if (input.TitleStatus ==
            TitleStatus.NotProvided)
        {
            steps.Add(
                "Confirm title status before listing the vehicle.");
        }

        if (repairMayPay)
        {
            steps.Add(
                "Compare a documented repair quote with the realistic increase in selling price before authorizing repairs.");
        }

        if (recommendation == "SELL NOW")
        {
            steps.Add(
                "Verify payment, title transfer, and buyer documentation before releasing the vehicle.");
        }
        else if (recommendation == "REPAIR FIRST")
        {
            steps.Add(
                "Repair only items whose expected selling-price improvement clearly exceeds their cost.");
        }
        else
        {
            steps.Add(
                "Rework the asking price, repair plan, or timing before committing to the sale.");
        }

        return steps;
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
            0,
            MidpointRounding.AwayFromZero);
    }

    private sealed record SellFactorScore(
        string Name,
        int Adjustment,
        string Explanation);
}
