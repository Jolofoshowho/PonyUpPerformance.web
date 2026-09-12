using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class TradeScoringService : ITradeScoringService
{
    private const int BaseScore = 50;

    private const int PonyUpThreshold = 75;
    private const int CautionThreshold = 50;

    /*
     * TRADE SCORING AUTHORITY
     *
     * Net Trade Value ............... +/-20
     * Condition Differential ....... +/-8
     * Title Differential ........... +/-7
     * Accident Differential ........ +/-6
     * Mileage vs Age Differential .. +/-5
     * Runs / Drives Differential ... +/-4
     *
     * TOTAL ......................... +/-50
     */

    private const int ValueMaximum = 20;
    private const int ConditionMaximum = 8;
    private const int TitleMaximum = 7;
    private const int AccidentMaximum = 6;
    private const int MileageMaximum = 5;
    private const int ReadinessMaximum = 4;

    public TradeDecisionResult Analyze(
        TradeDecisionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        decimal? yourAdjustedValue =
            CalculateAdjustedValue(
                input.YourValue,
                input.YourEstimatedRepairCost);

        decimal? theirAdjustedValue =
            CalculateAdjustedValue(
                input.TheirValue,
                input.TheirEstimatedRepairCost);

        decimal? netTradePosition =
            CalculateNetTradePosition(
                input,
                yourAdjustedValue,
                theirAdjustedValue);

        var factors =
            new List<TradeFactorScore>
            {
                ScoreValuePosition(
                    input,
                    yourAdjustedValue,
                    theirAdjustedValue,
                    netTradePosition),

                ScoreConditionDifferential(
                    input),

                ScoreTitleDifferential(
                    input),

                ScoreAccidentDifferential(
                    input),

                ScoreMileageDifferential(
                    input),

                ScoreReadinessDifferential(
                    input)
            };

        int totalAdjustment =
            factors.Sum(
                x => x.Adjustment);

        int score =
            Math.Clamp(
                BaseScore +
                totalAdjustment,
                0,
                100);

        int confidenceScore =
            CalculateConfidenceScore(
                input);

        int riskScore =
            CalculateRiskScore(
                input,
                netTradePosition,
                confidenceScore);

        string recommendation =
            score >= PonyUpThreshold
                ? "PONY UP"
                : score >= CautionThreshold
                    ? "NEGOTIATE"
                    : "WALK AWAY";

        return new TradeDecisionResult
        {
            Score = score,

            Recommendation =
                recommendation,

            ConfidenceScore =
                confidenceScore,

            RiskLevel =
                DetermineRiskLevel(
                    riskScore),

            YourAdjustedValue =
                RoundCurrency(
                    yourAdjustedValue),

            TheirAdjustedValue =
                RoundCurrency(
                    theirAdjustedValue),

            NetTradePosition =
                RoundCurrency(
                    netTradePosition),

            FinancialImpact =
                BuildFinancialImpact(
                    input,
                    yourAdjustedValue,
                    theirAdjustedValue,
                    netTradePosition),

            Reasoning =
                BuildReasoning(
                    recommendation,
                    factors,
                    netTradePosition),

            NextSteps =
                BuildNextSteps(
                    input,
                    recommendation,
                    netTradePosition)
        };
    }

    private static TradeFactorScore ScoreValuePosition(
        TradeDecisionInput input,
        decimal? yourAdjustedValue,
        decimal? theirAdjustedValue,
        decimal? netTradePosition)
    {
        if (!yourAdjustedValue.HasValue ||
            !theirAdjustedValue.HasValue ||
            !netTradePosition.HasValue)
        {
            return CreateFactor(
                "Net Trade Value",
                0,
                ValueMaximum,
                "Both market values are needed to calculate the net trade position.");
        }

        decimal yourConsideration =
            yourAdjustedValue.Value +
            PositiveOrZero(
                input.CashYouAdd);

        decimal baseline =
            Math.Max(
                1000m,
                yourConsideration);

        decimal ratio =
            netTradePosition.Value /
            baseline;

        int adjustment =
            ratio switch
            {
                >= 0.25m => 20,
                >= 0.15m => 16,
                >= 0.10m => 12,
                >= 0.05m => 8,
                >= 0.02m => 4,
                >= -0.02m => 0,
                >= -0.05m => -4,
                >= -0.10m => -8,
                >= -0.15m => -12,
                >= -0.25m => -16,
                _ => -20
            };

        string explanation =
            netTradePosition.Value switch
            {
                > 0 =>
                    $"After repair exposure and cash, the trade position is approximately {netTradePosition.Value:C0} in your favor.",

                < 0 =>
                    $"After repair exposure and cash, the trade position is approximately {Math.Abs(netTradePosition.Value):C0} against you.",

                _ =>
                    "After repair exposure and cash, the trade is approximately even."
            };

        return CreateFactor(
            "Net Trade Value",
            adjustment,
            ValueMaximum,
            explanation);
    }

    private static TradeFactorScore ScoreConditionDifferential(
        TradeDecisionInput input)
    {
        int? yours =
            GetConditionQuality(
                input.YourCondition);

        int? theirs =
            GetConditionQuality(
                input.TheirCondition);

        if (!yours.HasValue ||
            !theirs.HasValue)
        {
            return CreateFactor(
                "Condition Differential",
                0,
                ConditionMaximum,
                "Condition was not provided for both vehicles.");
        }

        int adjustment =
            ScaleQualityDifference(
                theirs.Value,
                yours.Value,
                ConditionMaximum);

        return CreateFactor(
            "Condition Differential",
            adjustment,
            ConditionMaximum,
            $"Your vehicle is {FormatEnum(input.YourCondition)}; their vehicle is {FormatEnum(input.TheirCondition)}.");
    }

    private static TradeFactorScore ScoreTitleDifferential(
        TradeDecisionInput input)
    {
        int? yours =
            GetTitleQuality(
                input.YourTitleStatus);

        int? theirs =
            GetTitleQuality(
                input.TheirTitleStatus);

        if (!yours.HasValue ||
            !theirs.HasValue)
        {
            return CreateFactor(
                "Title Differential",
                0,
                TitleMaximum,
                "Title status was not provided for both vehicles.");
        }

        int adjustment =
            ScaleQualityDifference(
                theirs.Value,
                yours.Value,
                TitleMaximum);

        return CreateFactor(
            "Title Differential",
            adjustment,
            TitleMaximum,
            $"Your title is {FormatEnum(input.YourTitleStatus)}; their title is {FormatEnum(input.TheirTitleStatus)}.");
    }

    private static TradeFactorScore ScoreAccidentDifferential(
        TradeDecisionInput input)
    {
        int? yours =
            GetAccidentQuality(
                input.YourAccidentHistory);

        int? theirs =
            GetAccidentQuality(
                input.TheirAccidentHistory);

        if (!yours.HasValue ||
            !theirs.HasValue)
        {
            return CreateFactor(
                "Accident History Differential",
                0,
                AccidentMaximum,
                "Accident history was not provided for both vehicles.");
        }

        int adjustment =
            ScaleQualityDifference(
                theirs.Value,
                yours.Value,
                AccidentMaximum);

        return CreateFactor(
            "Accident History Differential",
            adjustment,
            AccidentMaximum,
            $"Your accident history is {FormatEnum(input.YourAccidentHistory)}; theirs is {FormatEnum(input.TheirAccidentHistory)}.");
    }

    private static TradeFactorScore ScoreMileageDifferential(
        TradeDecisionInput input)
    {
        int? yours =
            GetMileageQuality(
                input.YourYear,
                input.YourMileage);

        int? theirs =
            GetMileageQuality(
                input.TheirYear,
                input.TheirMileage);

        if (!yours.HasValue ||
            !theirs.HasValue)
        {
            return CreateFactor(
                "Mileage vs Age Differential",
                0,
                MileageMaximum,
                "Year and mileage were not available for both vehicles.");
        }

        int adjustment =
            ScaleQualityDifference(
                theirs.Value,
                yours.Value,
                MileageMaximum);

        return CreateFactor(
            "Mileage vs Age Differential",
            adjustment,
            MileageMaximum,
            "Mileage was normalized against vehicle age for both sides of the trade.");
    }

    private static TradeFactorScore ScoreReadinessDifferential(
        TradeDecisionInput input)
    {
        decimal totalDifference = 0m;
        int comparisons = 0;

        if (input.YourRuns.HasValue &&
            input.TheirRuns.HasValue)
        {
            totalDifference +=
                BoolScore(
                    input.TheirRuns.Value) -
                BoolScore(
                    input.YourRuns.Value);

            comparisons++;
        }

        if (input.YourDrives.HasValue &&
            input.TheirDrives.HasValue)
        {
            totalDifference +=
                BoolScore(
                    input.TheirDrives.Value) -
                BoolScore(
                    input.YourDrives.Value);

            comparisons++;
        }

        if (comparisons == 0)
        {
            return CreateFactor(
                "Runs / Drives Differential",
                0,
                ReadinessMaximum,
                "Comparable running and driving status was not provided.");
        }

        decimal normalized =
            totalDifference /
            comparisons;

        int adjustment =
            (int)Math.Round(
                normalized *
                ReadinessMaximum,
                MidpointRounding.AwayFromZero);

        adjustment =
            Math.Clamp(
                adjustment,
                -ReadinessMaximum,
                ReadinessMaximum);

        return CreateFactor(
            "Runs / Drives Differential",
            adjustment,
            ReadinessMaximum,
            "Running and driving status was compared directly between the two vehicles.");
    }

    private static decimal? CalculateAdjustedValue(
        decimal? marketValue,
        decimal? repairCost)
    {
        if (!marketValue.HasValue ||
            marketValue.Value <= 0)
        {
            return null;
        }

        decimal repairs =
            PositiveOrZero(
                repairCost);

        return Math.Max(
            0m,
            marketValue.Value -
            repairs);
    }

    private static decimal? CalculateNetTradePosition(
        TradeDecisionInput input,
        decimal? yourAdjustedValue,
        decimal? theirAdjustedValue)
    {
        if (!yourAdjustedValue.HasValue ||
            !theirAdjustedValue.HasValue)
        {
            return null;
        }

        decimal whatYouReceive =
            theirAdjustedValue.Value +
            PositiveOrZero(
                input.CashTheyAdd);

        decimal whatYouGive =
            yourAdjustedValue.Value +
            PositiveOrZero(
                input.CashYouAdd);

        return whatYouReceive -
               whatYouGive;
    }

    private static int CalculateConfidenceScore(
        TradeDecisionInput input)
    {
        int confidence = 0;

        if (input.YourValue.HasValue &&
            input.TheirValue.HasValue)
        {
            confidence += 30;
        }

        if (input.YourCondition !=
                MechanicalCondition.NotProvided &&
            input.TheirCondition !=
                MechanicalCondition.NotProvided)
        {
            confidence += 12;
        }

        if (input.YourTitleStatus !=
                TitleStatus.NotProvided &&
            input.TheirTitleStatus !=
                TitleStatus.NotProvided)
        {
            confidence += 12;
        }

        if (input.YourAccidentHistory !=
                AccidentHistory.NotProvided &&
            input.TheirAccidentHistory !=
                AccidentHistory.NotProvided)
        {
            confidence += 10;
        }

        if (input.YourYear.HasValue &&
            input.TheirYear.HasValue &&
            input.YourMileage.HasValue &&
            input.TheirMileage.HasValue)
        {
            confidence += 12;
        }

        if (input.YourRuns.HasValue &&
            input.TheirRuns.HasValue &&
            input.YourDrives.HasValue &&
            input.TheirDrives.HasValue)
        {
            confidence += 8;
        }

        bool yourIdentity =
            input.YourYear.HasValue &&
            !string.IsNullOrWhiteSpace(
                input.YourMake) &&
            !string.IsNullOrWhiteSpace(
                input.YourModel);

        bool theirIdentity =
            input.TheirYear.HasValue &&
            !string.IsNullOrWhiteSpace(
                input.TheirMake) &&
            !string.IsNullOrWhiteSpace(
                input.TheirModel);

        if (yourIdentity &&
            theirIdentity)
        {
            confidence += 8;
        }

        if (!string.IsNullOrWhiteSpace(
                input.YourVin) &&
            !string.IsNullOrWhiteSpace(
                input.TheirVin))
        {
            confidence += 8;
        }

        return Math.Clamp(
            confidence,
            0,
            100);
    }

    private static int CalculateRiskScore(
        TradeDecisionInput input,
        decimal? netTradePosition,
        int confidenceScore)
    {
        int risk = 0;

        risk +=
            input.TheirCondition switch
            {
                MechanicalCondition.Poor => 12,
                MechanicalCondition.Severe => 22,
                _ => 0
            };

        risk +=
            input.TheirTitleStatus switch
            {
                TitleStatus.Rebuilt => 10,
                TitleStatus.Salvage => 20,
                TitleStatus.Flood => 28,
                _ => 0
            };

        risk +=
            input.TheirAccidentHistory switch
            {
                AccidentHistory.Minor => 3,
                AccidentHistory.Moderate => 10,
                AccidentHistory.Major => 20,
                _ => 0
            };

        if (input.TheirRuns == false)
        {
            risk += 15;
        }

        if (input.TheirDrives == false)
        {
            risk += 12;
        }

        if (input.TheirValue.HasValue &&
            input.TheirValue.Value > 0 &&
            input.TheirEstimatedRepairCost.HasValue)
        {
            decimal repairRatio =
                PositiveOrZero(
                    input.TheirEstimatedRepairCost) /
                input.TheirValue.Value;

            risk +=
                repairRatio switch
                {
                    <= 0.05m => 0,
                    <= 0.10m => 4,
                    <= 0.20m => 8,
                    <= 0.35m => 14,
                    _ => 20
                };
        }

        if (netTradePosition.HasValue &&
            netTradePosition.Value < 0)
        {
            risk += 10;
        }

        if (confidenceScore < 40)
        {
            risk += 18;
        }
        else if (confidenceScore < 65)
        {
            risk += 10;
        }
        else if (confidenceScore < 85)
        {
            risk += 4;
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
        TradeDecisionInput input,
        decimal? yourAdjustedValue,
        decimal? theirAdjustedValue,
        decimal? netTradePosition)
    {
        if (!yourAdjustedValue.HasValue ||
            !theirAdjustedValue.HasValue ||
            !netTradePosition.HasValue)
        {
            return
                "Enter both market values to calculate the net trade position.";
        }

        string position =
            netTradePosition.Value switch
            {
                > 0 =>
                    $"{netTradePosition.Value:C0} in your favor",

                < 0 =>
                    $"{Math.Abs(netTradePosition.Value):C0} against you",

                _ =>
                    "approximately even"
            };

        return
            $"Your adjusted vehicle value is {yourAdjustedValue.Value:C0}. " +
            $"Their adjusted vehicle value is {theirAdjustedValue.Value:C0}. " +
            $"After cash added by either side, the net trade position is {position}.";
    }

    private static string BuildReasoning(
        string recommendation,
        IEnumerable<TradeFactorScore> factors,
        decimal? netTradePosition)
    {
        List<TradeFactorScore> strongest =
            factors
                .Where(
                    x => x.Adjustment != 0)
                .OrderByDescending(
                    x => Math.Abs(
                        x.Adjustment))
                .Take(4)
                .ToList();

        string detail =
            strongest.Count == 0
                ? "There is not enough comparative evidence for strong factor adjustments."
                : string.Join(
                    " ",
                    strongest.Select(
                        x =>
                            $"{x.Name}: {x.Explanation}"));

        return
            $"{recommendation}. {detail}";
    }

    private static List<string> BuildNextSteps(
        TradeDecisionInput input,
        string recommendation,
        decimal? netTradePosition)
    {
        var steps =
            new List<string>();

        if (!input.YourValue.HasValue ||
            !input.TheirValue.HasValue)
        {
            steps.Add(
                "Confirm realistic market values for both vehicles.");
        }

        if (input.TheirTitleStatus ==
            TitleStatus.NotProvided)
        {
            steps.Add(
                "Verify the other vehicle's title status before trading.");
        }

        if (input.TheirAccidentHistory ==
            AccidentHistory.NotProvided)
        {
            steps.Add(
                "Verify accident history before committing to the trade.");
        }

        if (input.TheirEstimatedRepairCost.HasValue &&
            input.TheirEstimatedRepairCost.Value > 0)
        {
            steps.Add(
                "Verify the estimated repair exposure with an inspection or written repair estimate.");
        }

        if (netTradePosition.HasValue &&
            netTradePosition.Value < 0)
        {
            steps.Add(
                "Negotiate additional cash or value to close the current trade deficit.");
        }

        if (recommendation == "PONY UP")
        {
            steps.Add(
                "Confirm paperwork, VIN, title, and vehicle condition before exchanging ownership.");
        }
        else if (recommendation == "NEGOTIATE")
        {
            steps.Add(
                "Improve the cash difference or reduce uncertainty before completing the trade.");
        }
        else
        {
            steps.Add(
                "Do not complete the trade without materially improving the value or risk position.");
        }

        return steps
            .Distinct()
            .ToList();
    }

    private static int ScaleQualityDifference(
        int theirQuality,
        int yourQuality,
        int maximum)
    {
        decimal difference =
            (theirQuality -
             yourQuality) /
            100m;

        return Math.Clamp(
            (int)Math.Round(
                difference *
                maximum,
                MidpointRounding.AwayFromZero),
            -maximum,
            maximum);
    }

    private static int? GetConditionQuality(
        MechanicalCondition condition)
    {
        return condition switch
        {
            MechanicalCondition.Excellent => 100,
            MechanicalCondition.Good => 75,
            MechanicalCondition.Fair => 50,
            MechanicalCondition.Poor => 20,
            MechanicalCondition.Severe => 0,
            _ => null
        };
    }

    private static int? GetTitleQuality(
        TitleStatus title)
    {
        return title switch
        {
            TitleStatus.Clean => 100,
            TitleStatus.Rebuilt => 50,
            TitleStatus.Salvage => 20,
            TitleStatus.Flood => 0,
            _ => null
        };
    }

    private static int? GetAccidentQuality(
        AccidentHistory accident)
    {
        return accident switch
        {
            AccidentHistory.None => 100,
            AccidentHistory.Minor => 75,
            AccidentHistory.Moderate => 40,
            AccidentHistory.Major => 0,
            _ => null
        };
    }

    private static int? GetMileageQuality(
        int? year,
        int? mileage)
    {
        if (!year.HasValue ||
            !mileage.HasValue ||
            mileage.Value < 0)
        {
            return null;
        }

        int age =
            Math.Max(
                1,
                DateTime.UtcNow.Year -
                year.Value);

        decimal expectedMileage =
            age * 12_000m;

        decimal ratio =
            mileage.Value /
            expectedMileage;

        return ratio switch
        {
            <= 0.50m => 100,
            <= 0.70m => 85,
            <= 0.85m => 70,
            <= 1.00m => 55,
            <= 1.15m => 45,
            <= 1.35m => 30,
            <= 1.60m => 15,
            _ => 0
        };
    }

    private static decimal BoolScore(
        bool value)
    {
        return value
            ? 1m
            : 0m;
    }

    private static decimal PositiveOrZero(
        decimal? value)
    {
        if (!value.HasValue ||
            value.Value <= 0)
        {
            return 0m;
        }

        return value.Value;
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

    private static string FormatEnum<T>(
        T value)
        where T : Enum
    {
        return value
            .ToString()
            .Replace(
                "NotProvided",
                "Not Provided");
    }

    private static TradeFactorScore CreateFactor(
        string name,
        int adjustment,
        int maximum,
        string explanation)
    {
        return new TradeFactorScore(
            name,
            Math.Clamp(
                adjustment,
                -maximum,
                maximum),
            explanation);
    }

    private sealed record TradeFactorScore(
        string Name,
        int Adjustment,
        string Explanation);
}
