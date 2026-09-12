using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services.Scoring;

public class SellScoringService : ISellScoringService
{
    private const int BaseScore = 50;

    private const int SellNowThreshold = 75;
    private const int StrategicThreshold = 50;

    public SellDecisionResult Analyze(
        SellDecisionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        WeightProfile profile =
            GetWeightProfile(
                input.IntendedUse);

        var adjustments =
            new List<SellScoreAdjustment>
            {
                ScoreCondition(
                    input,
                    profile.Condition),

                ScorePriceVsMarket(
                    input,
                    profile.Price),

                ScoreTitle(
                    input,
                    profile.Title),

                ScoreMileage(
                    input,
                    profile.Mileage),

                ScoreAccidentHistory(
                    input,
                    profile.Accident),

                ScoreRuns(
                    input,
                    profile.Runs),

                ScoreDrives(
                    input,
                    profile.Drives)
            };

        int totalAdjustment =
            adjustments.Sum(
                x => x.Adjustment);

        int score =
            Math.Clamp(
                BaseScore +
                totalAdjustment,
                0,
                100);

        int confidenceScore =
            CalculateConfidenceScore(
                input,
                profile);

        int riskScore =
            CalculateRiskScore(
                adjustments,
                confidenceScore);

        string recommendation =
            DetermineRecommendation(score);

        decimal? salePriceVsMarketPercent =
            null;

        decimal? salePriceVsMarketDifference =
            null;

        if (input.ExpectedSalePrice.HasValue &&
            input.MarketValue.HasValue &&
            input.MarketValue.Value > 0)
        {
            salePriceVsMarketDifference =
                input.ExpectedSalePrice.Value -
                input.MarketValue.Value;

            salePriceVsMarketPercent =
                input.ExpectedSalePrice.Value /
                input.MarketValue.Value *
                100m;
        }

        return new SellDecisionResult
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

            SalePriceVsMarketPercent =
                salePriceVsMarketPercent.HasValue
                    ? Math.Round(
                        salePriceVsMarketPercent.Value,
                        1,
                        MidpointRounding.AwayFromZero)
                    : null,

            SalePriceVsMarketDifference =
                RoundCurrency(
                    salePriceVsMarketDifference),

            FinancialImpact =
                BuildFinancialImpact(
                    input,
                    salePriceVsMarketDifference),

            Reasoning =
                BuildReasoning(
                    recommendation,
                    profile,
                    adjustments),

            NextSteps =
                BuildNextSteps(
                    input,
                    recommendation)
        };
    }

    private static WeightProfile GetWeightProfile(
        SellIntendedUse intendedUse)
    {
        /*
         * TRANSPORT PROFILE
         *
         * Runs and drives matter heavily because
         * transportation is the intended purpose.
         *
         * Total authority = 50.
         */
        if (intendedUse is
            SellIntendedUse.DailyDriver
            or SellIntendedUse.WorkVehicle
            or SellIntendedUse.FamilyVehicle)
        {
            return new WeightProfile(
                "DAILY / FAMILY / WORK",
                Condition: 8,
                Price: 8,
                Title: 7,
                Mileage: 6,
                Accident: 5,
                Runs: 8,
                Drives: 8);
        }

        /*
         * BUILD PROFILE
         *
         * Running/driving status matters much less
         * because drivetrain replacement or major
         * mechanical work may already be planned.
         *
         * Condition, title and accident history
         * become substantially more important.
         *
         * Total authority = 50.
         */
        if (intendedUse is
            SellIntendedUse.ProjectVehicle
            or SellIntendedUse.PerformanceBuild
            or SellIntendedUse.Restoration)
        {
            return new WeightProfile(
                "PROJECT / PERFORMANCE / RESTORATION",
                Condition: 11,
                Price: 11,
                Title: 11,
                Mileage: 5,
                Accident: 8,
                Runs: 2,
                Drives: 2);
        }

        /*
         * NEUTRAL PROFILE
         *
         * Used only when Intended Use is blank.
         * Do not assume the vehicle is either a
         * commuter or a project.
         *
         * Total authority = 50.
         */
        return new WeightProfile(
            "GENERAL / NOT PROVIDED",
            Condition: 10,
            Price: 10,
            Title: 9,
            Mileage: 6,
            Accident: 5,
            Runs: 5,
            Drives: 5);
    }

    private static SellScoreAdjustment ScoreCondition(
        SellDecisionInput input,
        int maximum)
    {
        decimal multiplier =
            input.Condition switch
            {
                SellCondition.Excellent => 1.00m,
                SellCondition.Good => 0.60m,
                SellCondition.Fair => 0m,
                SellCondition.Poor => -0.60m,
                SellCondition.Severe => -1.00m,
                _ => 0m
            };

        int adjustment =
            Scale(
                maximum,
                multiplier);

        string explanation =
            input.Condition ==
            SellCondition.NotProvided
                ? "Overall condition was not provided."
                : $"Overall vehicle condition is {FormatEnum(input.Condition)}.";

        return CreateAdjustment(
            "Condition",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScorePriceVsMarket(
        SellDecisionInput input,
        int maximum)
    {
        if (!input.ExpectedSalePrice.HasValue ||
            !input.MarketValue.HasValue ||
            input.MarketValue.Value <= 0)
        {
            return CreateAdjustment(
                "Asking Price vs Market Value",
                0,
                maximum,
                "Sale price and market value were not both available.");
        }

        decimal ratio =
            input.ExpectedSalePrice.Value /
            input.MarketValue.Value;

        decimal multiplier =
            ratio switch
            {
                >= 1.05m => 1.00m,
                >= 0.98m => 0.75m,
                >= 0.92m => 0.50m,
                >= 0.85m => 0.25m,
                >= 0.75m => -0.40m,
                >= 0.65m => -0.70m,
                _ => -1.00m
            };

        int adjustment =
            Scale(
                maximum,
                multiplier);

        string explanation =
            $"Expected sale price is approximately {ratio:P0} of estimated market value.";

        return CreateAdjustment(
            "Asking Price vs Market Value",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScoreTitle(
        SellDecisionInput input,
        int maximum)
    {
        decimal multiplier =
            input.TitleStatus switch
            {
                TitleStatus.Clean => 1.00m,
                TitleStatus.Rebuilt => -0.45m,
                TitleStatus.Salvage => -0.75m,
                TitleStatus.Flood => -1.00m,
                _ => 0m
            };

        int adjustment =
            Scale(
                maximum,
                multiplier);

        string explanation =
            input.TitleStatus switch
            {
                TitleStatus.Clean =>
                    "The vehicle has a clean title.",

                TitleStatus.Rebuilt =>
                    "A rebuilt title reduces the available buyer pool and resale strength.",

                TitleStatus.Salvage =>
                    "A salvage title substantially affects buyer demand and market value.",

                TitleStatus.Flood =>
                    "A flood title creates severe resale and long-term condition concerns.",

                _ =>
                    "Title status was not provided."
            };

        return CreateAdjustment(
            "Title Status",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScoreMileage(
        SellDecisionInput input,
        int maximum)
    {
        if (!input.Year.HasValue ||
            !input.Mileage.HasValue)
        {
            return CreateAdjustment(
                "Mileage vs Age",
                0,
                maximum,
                "Year and mileage were not both provided.");
        }

        int vehicleAge =
            Math.Max(
                1,
                DateTime.UtcNow.Year -
                input.Year.Value);

        decimal expectedMileage =
            vehicleAge *
            12_000m;

        decimal ratio =
            input.Mileage.Value /
            expectedMileage;

        decimal multiplier =
            ratio switch
            {
                <= 0.50m => 1.00m,
                <= 0.70m => 0.75m,
                <= 0.85m => 0.50m,
                <= 1.00m => 0.25m,
                <= 1.15m => 0m,
                <= 1.35m => -0.40m,
                <= 1.60m => -0.75m,
                _ => -1.00m
            };

        int adjustment =
            Scale(
                maximum,
                multiplier);

        decimal annualMileage =
            input.Mileage.Value /
            (decimal)vehicleAge;

        string explanation =
            $"The vehicle averages approximately {annualMileage:N0} miles per year against a 12,000-mile annual benchmark.";

        return CreateAdjustment(
            "Mileage vs Age",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScoreAccidentHistory(
        SellDecisionInput input,
        int maximum)
    {
        decimal multiplier =
            input.AccidentHistory switch
            {
                AccidentHistory.None => 1.00m,
                AccidentHistory.Minor => 0.25m,
                AccidentHistory.Moderate => -0.40m,
                AccidentHistory.Major => -1.00m,
                _ => 0m
            };

        int adjustment =
            Scale(
                maximum,
                multiplier);

        string explanation =
            input.AccidentHistory switch
            {
                AccidentHistory.None =>
                    "No known accident history was reported.",

                AccidentHistory.Minor =>
                    "Minor accident history has limited impact when repairs were completed correctly.",

                AccidentHistory.Moderate =>
                    "Moderate accident history can affect body integrity, alignment and resale demand.",

                AccidentHistory.Major =>
                    "Major accident history can substantially affect structural integrity and buyer demand.",

                _ =>
                    "Accident history was not provided."
            };

        return CreateAdjustment(
            "Accident History",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScoreRuns(
        SellDecisionInput input,
        int maximum)
    {
        if (!input.Runs.HasValue)
        {
            return CreateAdjustment(
                "Runs",
                0,
                maximum,
                "Running status was not provided.");
        }

        int adjustment =
            input.Runs.Value
                ? maximum
                : -maximum;

        string explanation =
            input.Runs.Value
                ? "The vehicle currently runs."
                : "The vehicle currently does not run.";

        return CreateAdjustment(
            "Runs",
            adjustment,
            maximum,
            explanation);
    }

    private static SellScoreAdjustment ScoreDrives(
        SellDecisionInput input,
        int maximum)
    {
        if (!input.Drives.HasValue)
        {
            return CreateAdjustment(
                "Drives",
                0,
                maximum,
                "Driving status was not provided.");
        }

        int adjustment =
            input.Drives.Value
                ? maximum
                : -maximum;

        string explanation =
            input.Drives.Value
                ? "The vehicle currently drives."
                : "The vehicle currently does not drive.";

        return CreateAdjustment(
            "Drives",
            adjustment,
            maximum,
            explanation);
    }

    private static int CalculateConfidenceScore(
        SellDecisionInput input,
        WeightProfile profile)
    {
        int evidenceWeight = 0;

        if (input.Condition !=
            SellCondition.NotProvided)
        {
            evidenceWeight +=
                profile.Condition;
        }

        if (input.ExpectedSalePrice.HasValue &&
            input.MarketValue.HasValue &&
            input.MarketValue.Value > 0)
        {
            evidenceWeight +=
                profile.Price;
        }

        if (input.TitleStatus !=
            TitleStatus.NotProvided)
        {
            evidenceWeight +=
                profile.Title;
        }

        if (input.Year.HasValue &&
            input.Mileage.HasValue)
        {
            evidenceWeight +=
                profile.Mileage;
        }

        if (input.AccidentHistory !=
            AccidentHistory.NotProvided)
        {
            evidenceWeight +=
                profile.Accident;
        }

        if (input.Runs.HasValue)
        {
            evidenceWeight +=
                profile.Runs;
        }

        if (input.Drives.HasValue)
        {
            evidenceWeight +=
                profile.Drives;
        }

        /*
         * Factor evidence accounts for 90%.
         * Vehicle identity contributes the final 10%.
         */
        int confidence =
            (int)Math.Round(
                evidenceWeight /
                50m *
                90m,
                MidpointRounding.AwayFromZero);

        if (!string.IsNullOrWhiteSpace(input.Vin))
        {
            confidence += 4;
        }

        if (input.Year.HasValue)
        {
            confidence += 2;
        }

        if (!string.IsNullOrWhiteSpace(input.Make))
        {
            confidence += 2;
        }

        if (!string.IsNullOrWhiteSpace(input.Model))
        {
            confidence += 2;
        }

        return Math.Clamp(
            confidence,
            0,
            100);
    }

    private static int CalculateRiskScore(
        IEnumerable<SellScoreAdjustment> adjustments,
        int confidenceScore)
    {
        int negativeFactorRisk =
            adjustments
                .Where(
                    x => x.Adjustment < 0)
                .Sum(
                    x => Math.Abs(
                        x.Adjustment));

        int confidenceRisk =
            (int)Math.Round(
                (100 -
                 confidenceScore) /
                2m,
                MidpointRounding.AwayFromZero);

        return Math.Clamp(
            negativeFactorRisk +
            confidenceRisk,
            0,
            100);
    }

    private static string DetermineRecommendation(
        int score)
    {
        if (score >=
            SellNowThreshold)
        {
            return "SELL NOW";
        }

        if (score >=
            StrategicThreshold)
        {
            return "REPAIR / PRICE STRATEGICALLY";
        }

        return "HOLD / REWORK";
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
        decimal? difference)
    {
        if (!input.ExpectedSalePrice.HasValue ||
            !input.MarketValue.HasValue ||
            !difference.HasValue)
        {
            return
                "Add both an expected sale price and estimated market value for a direct seller-position comparison.";
        }

        if (difference.Value > 0)
        {
            return
                $"The expected sale price is approximately {difference.Value:C0} above estimated market value.";
        }

        if (difference.Value < 0)
        {
            return
                $"The expected sale price is approximately {Math.Abs(difference.Value):C0} below estimated market value.";
        }

        return
            "The expected sale price is approximately equal to estimated market value.";
    }

    private static string BuildReasoning(
        string recommendation,
        WeightProfile profile,
        IEnumerable<SellScoreAdjustment> adjustments)
    {
        string factors =
            string.Join(
                " ",
                adjustments.Select(
                    factor =>
                        $"{factor.Name}: {factor.Explanation}"));

        return
            $"{recommendation}. " +
            $"PonyUp used the {profile.Name} weighting profile. " +
            factors;
    }

    private static List<string> BuildNextSteps(
        SellDecisionInput input,
        string recommendation)
    {
        var steps =
            new List<string>();

        if (!input.MarketValue.HasValue)
        {
            steps.Add(
                "Establish a realistic current market value before setting or accepting a sale price.");
        }

        if (!input.ExpectedSalePrice.HasValue)
        {
            steps.Add(
                "Set a realistic asking or expected sale price.");
        }

        if (input.TitleStatus ==
            TitleStatus.NotProvided)
        {
            steps.Add(
                "Confirm title status before listing or accepting an offer.");
        }

        if (input.AccidentHistory ==
            AccidentHistory.NotProvided)
        {
            steps.Add(
                "Confirm accident history when reliable vehicle-history data is available.");
        }

        if (input.IntendedUse ==
            SellIntendedUse.NotProvided)
        {
            steps.Add(
                "Select the likely buyer use so PonyUp can weight running, driving, condition and title factors correctly.");
        }

        if (recommendation ==
            "SELL NOW")
        {
            steps.Add(
                "Verify payment and title-transfer documentation before releasing the vehicle.");
        }
        else if (recommendation ==
                 "REPAIR / PRICE STRATEGICALLY")
        {
            steps.Add(
                "Compare the likely sale-price improvement against any repair or preparation expense before spending more money.");
        }
        else
        {
            steps.Add(
                "Rework price, condition, timing or target buyer before committing to the sale.");
        }

        return steps;
    }

    private static SellScoreAdjustment CreateAdjustment(
        string name,
        int adjustment,
        int maximum,
        string explanation)
    {
        return new SellScoreAdjustment(
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
                "Vehicle",
                " Vehicle")
            .Replace(
                "Driver",
                " Driver")
            .Replace(
                "Build",
                " Build");
    }

    private sealed record WeightProfile(
        string Name,
        int Condition,
        int Price,
        int Title,
        int Mileage,
        int Accident,
        int Runs,
        int Drives);

    private sealed record SellScoreAdjustment(
        string Name,
        int Adjustment,
        int Maximum,
        string Explanation);
}
