using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class RepairScoringService :
    IRepairScoringService
{
    private const int BaseScore = 50;

    private const int PonyUpThreshold = 75;
    private const int CautionThreshold = 50;

    /*
     * REPAIR SCORING AUTHORITY
     *
     * This preserves the relative importance of the
     * previous Repair model while moving Repair onto
     * the same base-50 decision architecture used by
     * the newer PonyUp analyzers.
     *
     * Repair Economics .............. up to +/-15
     * Overall Condition ............. up to +/-10
     * Safety Importance ............. up to +10
     * Mileage vs Age ................ up to +/-8
     * Vehicle Age ................... up to +/-5
     * Ownership Intent .............. up to +/-2
     *
     * Maximum positive authority .... +50
     *
     * Missing evidence is neutral to the Decision Score.
     * It lowers Confidence instead.
     */

    private const int RepairEconomicsMaximum = 15;
    private const int ConditionMaximum = 10;
    private const int SafetyMaximum = 10;
    private const int MileageMaximum = 8;
    private const int AgeMaximum = 5;
    private const int OwnershipMaximum = 2;

    public DecisionResult Analyze(
        RepairDecisionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var factors =
            new List<RepairFactorScore>
            {
                ScoreRepairEconomics(input),

                ScoreCondition(input),

                ScoreSafety(input),

                ScoreMileageVsAge(input),

                ScoreVehicleAge(input),

                ScoreOwnershipIntent(input)
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

        string riskLevel =
            DetermineRiskLevel(
                riskScore);

        return new DecisionResult
        {
            Score =
                score,

            Recommendation =
                recommendation,

            ConfidenceScore =
                confidenceScore,

            RiskLevel =
                riskLevel,

            FinancialImpact =
                BuildFinancialImpact(
                    input),

            Reasoning =
                BuildReasoning(
                    recommendation,
                    factors,
                    confidenceScore),

            NextSteps =
                BuildNextSteps(
                    input,
                    recommendation,
                    confidenceScore,
                    riskLevel)
        };
    }

    private static RepairFactorScore
        ScoreRepairEconomics(
            RepairDecisionInput input)
    {
        if (!input.RepairCost.HasValue ||
            !input.VehicleValue.HasValue ||
            input.VehicleValue.Value <= 0)
        {
            return CreateFactor(
                "Repair Cost vs Vehicle Value",
                0,
                RepairEconomicsMaximum,
                "Repair cost and vehicle value were not both available.");
        }

        decimal ratio =
            input.RepairCost.Value /
            input.VehicleValue.Value;

        int adjustment =
            ratio switch
            {
                <= 0.10m => 15,
                <= 0.20m => 12,
                <= 0.30m => 9,
                <= 0.40m => 5,
                <= 0.50m => 2,
                <= 0.75m => -4,
                <= 1.00m => -9,
                _ => -15
            };

        string explanation =
            $"Repair cost is approximately " +
            $"{ratio:P0} of estimated vehicle value.";

        return CreateFactor(
            "Repair Cost vs Vehicle Value",
            adjustment,
            RepairEconomicsMaximum,
            explanation);
    }

    private static RepairFactorScore
        ScoreCondition(
            RepairDecisionInput input)
    {
        int adjustment =
            input.Condition switch
            {
                MechanicalCondition.Excellent =>
                    10,

                MechanicalCondition.Good =>
                    6,

                MechanicalCondition.Fair =>
                    0,

                MechanicalCondition.Poor =>
                    -6,

                MechanicalCondition.Severe =>
                    -10,

                _ =>
                    0
            };

        string explanation =
            input.Condition ==
            MechanicalCondition.NotProvided
                ? "Overall vehicle condition was not provided."
                : $"Overall vehicle condition is " +
                  $"{FormatEnum(input.Condition)}.";

        return CreateFactor(
            "Overall Condition",
            adjustment,
            ConditionMaximum,
            explanation);
    }

    private static RepairFactorScore
        ScoreSafety(
            RepairDecisionInput input)
    {
        if (!input.IsSafetyCritical.HasValue)
        {
            return CreateFactor(
                "Safety Importance",
                0,
                SafetyMaximum,
                "Safety importance was not provided.");
        }

        if (input.IsSafetyCritical.Value)
        {
            return CreateFactor(
                "Safety Importance",
                SafetyMaximum,
                SafetyMaximum,
                "The repair addresses a safety-critical concern.");
        }

        return CreateFactor(
            "Safety Importance",
            0,
            SafetyMaximum,
            "The repair was not identified as safety-critical.");
    }

    private static RepairFactorScore
        ScoreMileageVsAge(
            RepairDecisionInput input)
    {
        if (!input.VehicleYear.HasValue ||
            !input.Mileage.HasValue)
        {
            return CreateFactor(
                "Mileage vs Age",
                0,
                MileageMaximum,
                "Year and mileage were not both provided.");
        }

        int currentYear =
            DateTime.UtcNow.Year;

        int vehicleAge =
            Math.Max(
                1,
                currentYear -
                input.VehicleYear.Value);

        decimal expectedMileage =
            vehicleAge *
            12_000m;

        decimal ratio =
            input.Mileage.Value /
            expectedMileage;

        int adjustment =
            ratio switch
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

        decimal annualMileage =
            input.Mileage.Value /
            (decimal)vehicleAge;

        string explanation =
            $"The vehicle averages approximately " +
            $"{annualMileage:N0} miles per year " +
            $"against PonyUp's 12,000-mile annual benchmark.";

        return CreateFactor(
            "Mileage vs Age",
            adjustment,
            MileageMaximum,
            explanation);
    }

    private static RepairFactorScore
        ScoreVehicleAge(
            RepairDecisionInput input)
    {
        if (!input.VehicleYear.HasValue)
        {
            return CreateFactor(
                "Vehicle Age",
                0,
                AgeMaximum,
                "Vehicle year was not provided.");
        }

        int currentYear =
            DateTime.UtcNow.Year;

        int age =
            Math.Max(
                0,
                currentYear -
                input.VehicleYear.Value);

        int adjustment =
            age switch
            {
                <= 3 => 5,
                <= 7 => 4,
                <= 12 => 2,
                <= 20 => 0,
                <= 30 => -2,
                _ => -5
            };

        string explanation =
            age == 1
                ? "The vehicle is approximately 1 year old."
                : $"The vehicle is approximately {age} years old.";

        return CreateFactor(
            "Vehicle Age",
            adjustment,
            AgeMaximum,
            explanation);
    }

    private static RepairFactorScore
        ScoreOwnershipIntent(
            RepairDecisionInput input)
    {
        if (!input.OwnershipYears.HasValue)
        {
            return CreateFactor(
                "Ownership Intent",
                0,
                OwnershipMaximum,
                "Planned ownership period was not provided.");
        }

        int adjustment =
            input.OwnershipYears.Value switch
            {
                >= 5 => 2,
                >= 3 => 1,
                >= 1 => 0,
                _ => -2
            };

        string explanation =
            input.OwnershipYears.Value switch
            {
                0 =>
                    "The vehicle is not expected to remain in the owner's possession long enough to spread the repair cost over future use.",

                1 =>
                    "The planned ownership period is approximately 1 year.",

                _ =>
                    $"The planned ownership period is approximately " +
                    $"{input.OwnershipYears.Value} years."
            };

        return CreateFactor(
            "Ownership Intent",
            adjustment,
            OwnershipMaximum,
            explanation);
    }

    private static int CalculateConfidenceScore(
        RepairDecisionInput input)
    {
        int confidence = 0;

        /*
         * Confidence measures evidence completeness.
         *
         * Missing information does not lower the
         * Decision Score. It only lowers Confidence.
         */

        if (input.RepairCost.HasValue)
        {
            confidence += 20;
        }

        if (input.VehicleValue.HasValue &&
            input.VehicleValue.Value > 0)
        {
            confidence += 20;
        }

        if (input.Condition !=
            MechanicalCondition.NotProvided)
        {
            confidence += 15;
        }

        if (input.Mileage.HasValue)
        {
            confidence += 10;
        }

        if (input.VehicleYear.HasValue)
        {
            confidence += 10;
        }

        if (input.IsSafetyCritical.HasValue)
        {
            confidence += 10;
        }

        if (input.OwnershipYears.HasValue)
        {
            confidence += 5;
        }

        if (!string.IsNullOrWhiteSpace(
                input.VehicleMake))
        {
            confidence += 5;
        }

        if (!string.IsNullOrWhiteSpace(
                input.VehicleModel))
        {
            confidence += 5;
        }

        /*
         * VIN is optional.
         *
         * A supplied VIN may strengthen confidence,
         * but a customer does not need a VIN to reach
         * 100% confidence if all relevant manual
         * evidence is present.
         */
        if (!string.IsNullOrWhiteSpace(
                input.Vin))
        {
            confidence += 5;
        }

        return Math.Clamp(
            confidence,
            0,
            100);
    }

    private static int CalculateRiskScore(
        RepairDecisionInput input,
        IEnumerable<RepairFactorScore> factors,
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

        int safetyRisk =
            input.IsSafetyCritical == true
                ? 10
                : 0;

        return Math.Clamp(
            negativeFactorRisk +
            confidenceRisk +
            safetyRisk,
            0,
            100);
    }

    private static string DetermineRecommendation(
        int score)
    {
        if (score >=
            PonyUpThreshold)
        {
            return "PONY UP";
        }

        if (score >=
            CautionThreshold)
        {
            return "PROCEED WITH CAUTION";
        }

        return "WALK AWAY";
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
        RepairDecisionInput input)
    {
        if (input.RepairCost.HasValue &&
            input.VehicleValue.HasValue &&
            input.VehicleValue.Value > 0)
        {
            decimal ratio =
                input.RepairCost.Value /
                input.VehicleValue.Value;

            return
                $"The repair is approximately " +
                $"{input.RepairCost.Value:C0}, " +
                $"or {ratio:P0} of the vehicle's " +
                $"estimated {input.VehicleValue.Value:C0} value.";
        }

        if (input.RepairCost.HasValue)
        {
            return
                $"The repair cost is approximately " +
                $"{input.RepairCost.Value:C0}. " +
                $"Add vehicle value for a direct repair-to-value comparison.";
        }

        if (input.VehicleValue.HasValue &&
            input.VehicleValue.Value > 0)
        {
            return
                $"Estimated vehicle value is approximately " +
                $"{input.VehicleValue.Value:C0}. " +
                $"Add repair cost for a direct repair-to-value comparison.";
        }

        return
            "Repair cost and vehicle value were not both available, so the direct financial comparison remains unresolved.";
    }

    private static string BuildReasoning(
        string recommendation,
        IEnumerable<RepairFactorScore> factors,
        int confidenceScore)
    {
        List<RepairFactorScore> strongest =
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

        if (strongest.Count == 0)
        {
            return
                $"{recommendation}. " +
                $"Available evidence is limited, so the current score remains near neutral. " +
                $"Confidence is {confidenceScore}%.";
        }

        string factorSummary =
            string.Join(
                " ",
                strongest.Select(
                    factor =>
                        $"{factor.Name}: {factor.Explanation}"));

        return
            $"{recommendation}. " +
            $"{factorSummary} " +
            $"Confidence is {confidenceScore}%.";
    }

    private static List<string> BuildNextSteps(
        RepairDecisionInput input,
        string recommendation,
        int confidenceScore,
        string riskLevel)
    {
        var steps =
            new List<string>();

        if (!input.RepairCost.HasValue)
        {
            steps.Add(
                "Get a repair estimate or written shop quote to strengthen the decision.");
        }

        if (!input.VehicleValue.HasValue ||
            input.VehicleValue.Value <= 0)
        {
            steps.Add(
                "Establish a realistic vehicle value before committing substantial repair money.");
        }

        if (input.Condition ==
            MechanicalCondition.NotProvided)
        {
            steps.Add(
                "Assess the vehicle's overall mechanical condition before committing additional money.");
        }

        if (!input.Mileage.HasValue)
        {
            steps.Add(
                "Add current mileage to improve remaining-life context.");
        }

        if (input.IsSafetyCritical == true)
        {
            steps.Add(
                "Do not continue driving until the safety concern has been properly evaluated.");
        }

        if (recommendation ==
            "PONY UP")
        {
            steps.Add(
                "Confirm the final repair quote, parts quality, labor warranty, and repair scope before authorizing the work.");
        }
        else if (recommendation ==
                 "PROCEED WITH CAUTION")
        {
            steps.Add(
                "Get a second estimate and verify that no additional major mechanical or structural problems are being overlooked.");
        }
        else
        {
            steps.Add(
                "Compare repairing the vehicle against selling, trading, or replacing it before committing additional money.");
        }

        if (confidenceScore < 65)
        {
            steps.Add(
                "Add more vehicle and repair evidence before treating the recommendation as high-confidence.");
        }

        if (riskLevel ==
            "HIGH")
        {
            steps.Add(
                "Consider a complete inspection before committing additional money.");
        }

        return steps
            .Distinct()
            .ToList();
    }

    private static RepairFactorScore CreateFactor(
        string name,
        int adjustment,
        int maximum,
        string explanation)
    {
        return new RepairFactorScore(
            name,
            Math.Clamp(
                adjustment,
                -maximum,
                maximum),
            maximum,
            explanation);
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

    private sealed record RepairFactorScore(
        string Name,
        int Adjustment,
        int Maximum,
        string Explanation);
}
