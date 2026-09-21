using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class UpgradeScoringService :
    IUpgradeScoringService
{
    private const int BaseScore = 50;

    private const int WorthItThreshold = 75;
    private const int MarginalThreshold = 50;

    public UpgradeDecisionResult Analyze(
        UpgradeDecisionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        WeightProfile profile =
            GetWeightProfile(
                input.Goal);

        var factors =
            new List<UpgradeFactorScore>
            {
                ScoreCostBurden(
                    input,
                    profile.Cost),

                ScoreValueRecovery(
                    input,
                    profile.Value),

                ScorePerformance(
                    input,
                    profile.Performance),

                ScoreReliability(
                    input,
                    profile.Reliability),

                ScoreDailyDriverImpact(
                    input,
                    profile.DailyDriver)
            };

        int totalAdjustment =
            factors.Sum(
                factor =>
                    factor.Adjustment);

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
                factors,
                confidenceScore);

        string recommendation =
            DetermineRecommendation(
                score);

        decimal? costVsValuePercent =
            CalculateCostVsValuePercent(
                input);

        decimal? valueRecoveryPercent =
            CalculateValueRecoveryPercent(
                input);

        decimal? projectedVehicleValue =
            CalculateProjectedVehicleValue(
                input);

        decimal? netValueImpact =
            CalculateNetValueImpact(
                input);

        return new UpgradeDecisionResult
        {
            Score =
                score,

            Recommendation =
                recommendation,

            ConfidenceScore =
                confidenceScore,

            RiskLevel =
                DetermineRiskLevel(
                    riskScore),

            WeightingProfile =
                profile.Name,

            UpgradeCostVsVehicleValuePercent =
                costVsValuePercent,

            ValueRecoveryPercent =
                valueRecoveryPercent,

            ProjectedVehicleValue =
                RoundCurrency(
                    projectedVehicleValue),

            NetValueImpact =
                RoundCurrency(
                    netValueImpact),

            FinancialImpact =
                BuildFinancialImpact(
                    input),

            Reasoning =
                BuildReasoning(
                    recommendation,
                    profile,
                    factors,
                    confidenceScore),

            NextSteps =
                BuildNextSteps(
                    input,
                    recommendation,
                    confidenceScore)
        };
    }

    private static WeightProfile GetWeightProfile(
        UpgradeGoal goal)
    {
        if (goal ==
            UpgradeGoal.Performance)
        {
            return new WeightProfile(
                "PERFORMANCE",
                Cost: 15,
                Value: 10,
                Performance: 10,
                Reliability: 10,
                DailyDriver: 5);
        }

        if (goal ==
            UpgradeGoal.Reliability)
        {
            return new WeightProfile(
                "RELIABILITY",
                Cost: 15,
                Value: 10,
                Performance: 0,
                Reliability: 20,
                DailyDriver: 5);
        }

        if (goal ==
            UpgradeGoal.ResaleValue)
        {
            return new WeightProfile(
                "RESALE VALUE",
                Cost: 15,
                Value: 20,
                Performance: 0,
                Reliability: 10,
                DailyDriver: 5);
        }

        return new WeightProfile(
            "GENERAL",
            Cost: 15,
            Value: 15,
            Performance: 5,
            Reliability: 10,
            DailyDriver: 5);
    }

    private static UpgradeFactorScore ScoreCostBurden(
        UpgradeDecisionInput input,
        int maximum)
    {
        if (!input.UpgradeCost.HasValue ||
            !input.CurrentValue.HasValue ||
            input.CurrentValue.Value <= 0)
        {
            return CreateFactor(
                "Upgrade Cost vs Vehicle Value",
                0,
                maximum,
                "Upgrade cost and current vehicle value were not both available.");
        }

        decimal ratio =
            input.UpgradeCost.Value /
            input.CurrentValue.Value;

        decimal multiplier =
            ratio switch
            {
                <= 0.05m => 1.00m,
                <= 0.10m => 0.80m,
                <= 0.20m => 0.50m,
                <= 0.30m => 0.25m,
                <= 0.40m => 0m,
                <= 0.60m => -0.40m,
                <= 0.80m => -0.70m,
                _ => -1.00m
            };

        return CreateFactor(
            "Upgrade Cost vs Vehicle Value",
            Scale(
                maximum,
                multiplier),
            maximum,
            $"Upgrade cost equals approximately {ratio:P0} of current vehicle value.");
    }

    private static UpgradeFactorScore ScoreValueRecovery(
        UpgradeDecisionInput input,
        int maximum)
    {
        if (!input.UpgradeCost.HasValue ||
            input.UpgradeCost.Value <= 0 ||
            !input.ValueAdded.HasValue)
        {
            return CreateFactor(
                "Resale Value Recovery",
                0,
                maximum,
                "Upgrade cost and estimated resale value added were not both available.");
        }

        decimal ratio =
            input.ValueAdded.Value /
            input.UpgradeCost.Value;

        decimal multiplier =
            ratio switch
            {
                >= 1.00m => 1.00m,
                >= 0.75m => 0.75m,
                >= 0.50m => 0.50m,
                >= 0.25m => 0.20m,
                >= 0.10m => 0m,
                > 0m => -0.25m,
                _ => -0.50m
            };

        return CreateFactor(
            "Resale Value Recovery",
            Scale(
                maximum,
                multiplier),
            maximum,
            $"Estimated resale value recovery is approximately {ratio:P0} of upgrade cost.");
    }

    private static UpgradeFactorScore ScorePerformance(
        UpgradeDecisionInput input,
        int maximum)
    {
        if (maximum <= 0)
        {
            return CreateFactor(
                "Performance Gain",
                0,
                0,
                "Performance gain is not weighted for the selected upgrade goal.");
        }

        if (!input.HorsepowerGain.HasValue)
        {
            return CreateFactor(
                "Performance Gain",
                0,
                maximum,
                "Expected horsepower gain was not provided.");
        }

        decimal multiplier;

        string explanation;

        if (input.CurrentHorsepower.HasValue &&
            input.CurrentHorsepower.Value > 0)
        {
            decimal gainRatio =
                input.HorsepowerGain.Value /
                (decimal)input.CurrentHorsepower.Value;

            multiplier =
                gainRatio switch
                {
                    >= 0.30m => 1.00m,
                    >= 0.20m => 0.80m,
                    >= 0.10m => 0.50m,
                    >= 0.05m => 0.25m,
                    > 0m => 0.10m,
                    _ => 0m
                };

            explanation =
                $"Expected horsepower gain is approximately {gainRatio:P0} over current output.";
        }
        else
        {
            multiplier =
                input.HorsepowerGain.Value switch
                {
                    >= 100 => 1.00m,
                    >= 50 => 0.75m,
                    >= 25 => 0.50m,
                    >= 10 => 0.25m,
                    > 0 => 0.10m,
                    _ => 0m
                };

            explanation =
                $"Expected horsepower gain is approximately {input.HorsepowerGain.Value} HP.";
        }

        return CreateFactor(
            "Performance Gain",
            Scale(
                maximum,
                multiplier),
            maximum,
            explanation);
    }

    private static UpgradeFactorScore ScoreReliability(
        UpgradeDecisionInput input,
        int maximum)
    {
        decimal multiplier =
            input.ReliabilityImpact switch
            {
                UpgradeReliabilityImpact.MuchBetter =>
                    1.00m,

                UpgradeReliabilityImpact.Better =>
                    0.60m,

                UpgradeReliabilityImpact.NoChange =>
                    0m,

                UpgradeReliabilityImpact.Worse =>
                    -0.60m,

                UpgradeReliabilityImpact.MuchWorse =>
                    -1.00m,

                _ =>
                    0m
            };

        string explanation =
            input.ReliabilityImpact ==
            UpgradeReliabilityImpact.NotProvided
                ? "Expected reliability impact was not provided."
                : $"Expected reliability impact is {FormatEnum(input.ReliabilityImpact)}.";

        return CreateFactor(
            "Reliability Impact",
            Scale(
                maximum,
                multiplier),
            maximum,
            explanation);
    }

    private static UpgradeFactorScore ScoreDailyDriverImpact(
        UpgradeDecisionInput input,
        int maximum)
    {
        if (!input.IsDailyDriver.HasValue)
        {
            return CreateFactor(
                "Daily-Driver Impact",
                0,
                maximum,
                "Daily-driver status was not provided.");
        }

        if (!input.IsDailyDriver.Value)
        {
            return CreateFactor(
                "Daily-Driver Impact",
                Scale(
                    maximum,
                    0.40m),
                maximum,
                "The vehicle is not relied on as a daily driver, reducing the consequence of downtime or drivability compromises.");
        }

        decimal multiplier =
            input.ReliabilityImpact switch
            {
                UpgradeReliabilityImpact.MuchBetter =>
                    1.00m,

                UpgradeReliabilityImpact.Better =>
                    0.60m,

                UpgradeReliabilityImpact.NoChange =>
                    0m,

                UpgradeReliabilityImpact.Worse =>
                    -0.60m,

                UpgradeReliabilityImpact.MuchWorse =>
                    -1.00m,

                _ =>
                    0m
            };

        string explanation =
            input.ReliabilityImpact ==
            UpgradeReliabilityImpact.NotProvided
                ? "The vehicle is a daily driver, but reliability impact was not provided."
                : "Daily-driver use makes reliability and drivability consequences more important.";

        return CreateFactor(
            "Daily-Driver Impact",
            Scale(
                maximum,
                multiplier),
            maximum,
            explanation);
    }

    private static int CalculateConfidenceScore(
        UpgradeDecisionInput input)
    {
        bool performanceGoal =
            input.Goal ==
            UpgradeGoal.Performance;

        int confidence = 0;

        if (input.UpgradeCost.HasValue)
        {
            confidence += 20;
        }

        if (input.CurrentValue.HasValue)
        {
            confidence += 15;
        }

        if (input.ValueAdded.HasValue)
        {
            confidence +=
                performanceGoal
                    ? 10
                    : 15;
        }

        if (input.Goal !=
            UpgradeGoal.NotProvided)
        {
            confidence += 10;
        }

        if (input.ReliabilityImpact !=
            UpgradeReliabilityImpact.NotProvided)
        {
            confidence += 15;
        }

        if (input.IsDailyDriver.HasValue)
        {
            confidence += 10;
        }

        int identityWeight =
            performanceGoal
                ? 10
                : 15;

        int identityFields = 0;

        if (input.Year.HasValue)
        {
            identityFields++;
        }

        if (!string.IsNullOrWhiteSpace(
                input.Make))
        {
            identityFields++;
        }

        if (!string.IsNullOrWhiteSpace(
                input.Model))
        {
            identityFields++;
        }

        confidence +=
            (int)Math.Round(
                identityWeight *
                identityFields /
                3m,
                MidpointRounding.AwayFromZero);

        if (performanceGoal &&
            input.HorsepowerGain.HasValue)
        {
            confidence += 10;
        }

        return Math.Clamp(
            confidence,
            0,
            100);
    }

    private static int CalculateRiskScore(
        UpgradeDecisionInput input,
        IEnumerable<UpgradeFactorScore> factors,
        int confidenceScore)
    {
        int negativeFactorRisk =
            factors
                .Where(
                    factor =>
                        factor.Adjustment < 0)
                .Sum(
                    factor =>
                        Math.Abs(
                            factor.Adjustment));

        int confidenceRisk =
            (int)Math.Round(
                (100 -
                 confidenceScore) /
                2m,
                MidpointRounding.AwayFromZero);

        int reliabilityRisk =
            input.ReliabilityImpact switch
            {
                UpgradeReliabilityImpact.MuchWorse =>
                    15,

                UpgradeReliabilityImpact.Worse =>
                    8,

                _ =>
                    0
            };

        int dailyDriverRisk =
            input.IsDailyDriver == true &&
            input.ReliabilityImpact is
                UpgradeReliabilityImpact.Worse
                or UpgradeReliabilityImpact.MuchWorse
                    ? 5
                    : 0;

        return Math.Clamp(
            negativeFactorRisk +
            confidenceRisk +
            reliabilityRisk +
            dailyDriverRisk,
            0,
            100);
    }

    private static string DetermineRecommendation(
        int score)
    {
        if (score >=
            WorthItThreshold)
        {
            return "WORTH IT";
        }

        if (score >=
            MarginalThreshold)
        {
            return "MARGINAL";
        }

        return "POOR FINANCIAL MOVE";
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

    private static decimal? CalculateCostVsValuePercent(
        UpgradeDecisionInput input)
    {
        if (!input.UpgradeCost.HasValue ||
            !input.CurrentValue.HasValue ||
            input.CurrentValue.Value <= 0)
        {
            return null;
        }

        return Math.Round(
            input.UpgradeCost.Value /
            input.CurrentValue.Value *
            100m,
            1,
            MidpointRounding.AwayFromZero);
    }

    private static decimal? CalculateValueRecoveryPercent(
        UpgradeDecisionInput input)
    {
        if (!input.UpgradeCost.HasValue ||
            input.UpgradeCost.Value <= 0 ||
            !input.ValueAdded.HasValue)
        {
            return null;
        }

        return Math.Round(
            input.ValueAdded.Value /
            input.UpgradeCost.Value *
            100m,
            1,
            MidpointRounding.AwayFromZero);
    }

    private static decimal? CalculateProjectedVehicleValue(
        UpgradeDecisionInput input)
    {
        if (!input.CurrentValue.HasValue ||
            !input.ValueAdded.HasValue)
        {
            return null;
        }

        return
            input.CurrentValue.Value +
            input.ValueAdded.Value;
    }

    private static decimal? CalculateNetValueImpact(
        UpgradeDecisionInput input)
    {
        if (!input.UpgradeCost.HasValue ||
            !input.ValueAdded.HasValue)
        {
            return null;
        }

        return
            input.ValueAdded.Value -
            input.UpgradeCost.Value;
    }

    private static string BuildFinancialImpact(
        UpgradeDecisionInput input)
    {
        if (input.UpgradeCost.HasValue &&
            input.ValueAdded.HasValue)
        {
            decimal net =
                input.ValueAdded.Value -
                input.UpgradeCost.Value;

            decimal recovery =
                input.UpgradeCost.Value > 0
                    ? input.ValueAdded.Value /
                      input.UpgradeCost.Value
                    : 0m;

            if (net >= 0)
            {
                return
                    $"The upgrade costs approximately {input.UpgradeCost.Value:C0} " +
                    $"and is estimated to add {input.ValueAdded.Value:C0} in vehicle value, " +
                    $"recovering approximately {recovery:P0} of the spend.";
            }

            return
                $"The upgrade costs approximately {input.UpgradeCost.Value:C0} " +
                $"and is estimated to add {input.ValueAdded.Value:C0} in vehicle value. " +
                $"Approximately {Math.Abs(net):C0} of the spend is not recovered through estimated resale value.";
        }

        if (input.UpgradeCost.HasValue)
        {
            return
                $"The known upgrade cost is approximately {input.UpgradeCost.Value:C0}. " +
                $"Add estimated resale value impact for a stronger financial comparison.";
        }

        return
            "Upgrade economics are incomplete because upgrade cost and resale impact were not both available.";
    }

    private static string BuildReasoning(
        string recommendation,
        WeightProfile profile,
        IEnumerable<UpgradeFactorScore> factors,
        int confidenceScore)
    {
        List<UpgradeFactorScore> strongest =
            factors
                .Where(
                    factor =>
                        factor.Adjustment != 0)
                .OrderByDescending(
                    factor =>
                        Math.Abs(
                            factor.Adjustment))
                .Take(3)
                .ToList();

        string profileText =
            $"PonyUp used the {profile.Name} upgrade profile.";

        if (strongest.Count == 0)
        {
            return
                $"{recommendation}. {profileText} " +
                $"Available evidence is limited, so the score remains near neutral. " +
                $"Confidence is {confidenceScore}%.";
        }

        string factorText =
            string.Join(
                " ",
                strongest.Select(
                    factor =>
                        $"{factor.Name}: {factor.Explanation}"));

        return
            $"{recommendation}. {profileText} " +
            $"{factorText} " +
            $"Confidence is {confidenceScore}%.";
    }

    private static List<string> BuildNextSteps(
        UpgradeDecisionInput input,
        string recommendation,
        int confidenceScore)
    {
        var steps =
            new List<string>();

        if (!input.UpgradeCost.HasValue)
        {
            steps.Add(
                "Establish the complete installed cost before committing to the upgrade.");
        }

        if (!input.CurrentValue.HasValue)
        {
            steps.Add(
                "Establish the vehicle's current value so the upgrade can be compared with the asset it is being installed on.");
        }

        if (!input.ValueAdded.HasValue)
        {
            steps.Add(
                "Estimate realistically how much of the upgrade cost would be reflected in resale value.");
        }

        if (input.Goal ==
            UpgradeGoal.Performance &&
            !input.HorsepowerGain.HasValue)
        {
            steps.Add(
                "Add a realistic expected horsepower gain for the planned performance upgrade.");
        }

        if (input.ReliabilityImpact ==
            UpgradeReliabilityImpact.NotProvided)
        {
            steps.Add(
                "Evaluate how the modification is expected to affect reliability before spending the money.");
        }

        if (input.IsDailyDriver == true &&
            input.ReliabilityImpact is
                UpgradeReliabilityImpact.Worse
                or UpgradeReliabilityImpact.MuchWorse)
        {
            steps.Add(
                "Account for downtime and daily-driver reliability before committing to the modification.");
        }

        if (recommendation ==
            "WORTH IT")
        {
            steps.Add(
                "Verify parts quality, labor cost, supporting modifications, and warranty implications before purchasing.");
        }
        else if (recommendation ==
                 "MARGINAL")
        {
            steps.Add(
                "Reduce cost, improve the parts plan, or verify stronger measurable benefits before committing.");
        }
        else
        {
            steps.Add(
                "Rework the upgrade plan before spending the money; the current economics or risk do not support the investment.");
        }

        if (confidenceScore < 65)
        {
            steps.Add(
                "Add more cost, value, reliability, and goal evidence before treating the recommendation as high-confidence.");
        }

        return steps
            .Distinct()
            .ToList();
    }

    private static UpgradeFactorScore CreateFactor(
        string name,
        int adjustment,
        int maximum,
        string explanation)
    {
        if (maximum <= 0)
        {
            return new UpgradeFactorScore(
                name,
                0,
                0,
                explanation);
        }

        return new UpgradeFactorScore(
            name,
            Math.Clamp(
                adjustment,
                -maximum,
                maximum),
            maximum,
            explanation);
    }

    private static int Scale(
        int maximum,
        decimal multiplier)
    {
        if (maximum <= 0)
        {
            return 0;
        }

        return Math.Clamp(
            (int)Math.Round(
                maximum *
                multiplier,
                MidpointRounding.AwayFromZero),
            -maximum,
            maximum);
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
                "MuchBetter",
                "Much Better")
            .Replace(
                "MuchWorse",
                "Much Worse")
            .Replace(
                "NoChange",
                "No Change")
            .Replace(
                "ComfortTechnology",
                "Comfort / Technology")
            .Replace(
                "UtilityTowing",
                "Utility / Towing")
            .Replace(
                "FuelEconomy",
                "Fuel Economy")
            .Replace(
                "ResaleValue",
                "Resale Value");
    }

    private sealed record WeightProfile(
        string Name,
        int Cost,
        int Value,
        int Performance,
        int Reliability,
        int DailyDriver);

    private sealed record UpgradeFactorScore(
        string Name,
        int Adjustment,
        int Maximum,
        string Explanation);
}
