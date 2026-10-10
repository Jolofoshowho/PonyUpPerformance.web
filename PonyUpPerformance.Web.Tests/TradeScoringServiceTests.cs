using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services.Scoring;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class TradeScoringServiceTests
{
    [Fact]
    public void Analyze_RepresentativeTrade_ReturnsDecision()
    {
        var service =
            new TradeScoringService();

        var input =
            new TradeDecisionInput
            {
                YourYear = 2008,
                YourMake = "Pontiac",
                YourModel = "Grand Prix",
                YourValue = 6500m,
                YourMileage = 145000,
                YourCondition = MechanicalCondition.Good,
                YourTitleStatus = TitleStatus.Clean,
                YourAccidentHistory = AccidentHistory.None,
                YourRuns = true,
                YourDrives = true,

                TheirYear = 2012,
                TheirMake = "Chevrolet",
                TheirModel = "Impala",
                TheirValue = 8000m,
                TheirMileage = 120000,
                TheirCondition = MechanicalCondition.Good,
                TheirTitleStatus = TitleStatus.Clean,
                TheirAccidentHistory = AccidentHistory.None,
                TheirRuns = true,
                TheirDrives = true
            };

        TradeDecisionResult result =
            service.Analyze(input);

        Assert.InRange(
            result.Score,
            0,
            100);

        Assert.InRange(
            result.ConfidenceScore,
            0,
            100);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Recommendation));

        Assert.NotNull(
            result.YourAdjustedValue);

        Assert.NotNull(
            result.TheirAdjustedValue);

        Assert.NotNull(
            result.NetTradePosition);
    }

    [Fact]
    public void Analyze_NearEvenMoneyWithBetterConditionAndMileage_IsPonyUp()
    {
        var service =
            new TradeScoringService();

        var input =
            new TradeDecisionInput
            {
                YourYear = 2012,
                YourMake = "Chevrolet",
                YourModel = "Suburban",
                YourValue = 3700m,
                YourMileage = 345765,
                YourCondition = MechanicalCondition.Fair,

                TheirYear = 2006,
                TheirMake = "Pontiac",
                TheirModel = "Grand Prix",
                TheirValue = 3500m,
                TheirMileage = 234829,
                TheirCondition = MechanicalCondition.Excellent
            };

        TradeDecisionResult result =
            service.Analyze(input);

        Assert.Equal(
            "PONY UP",
            result.Recommendation);

        Assert.Equal(
            "LOW",
            result.RiskLevel);

        Assert.True(
            result.Score >= 55,
            $"Expected a green-light score, but received {result.Score}.");
    }

    [Fact]
    public void Analyze_EvenTradeWithoutQualityAdvantage_IsNegotiate()
    {
        var service =
            new TradeScoringService();

        var input =
            new TradeDecisionInput
            {
                YourValue = 5000m,
                TheirValue = 5000m,
                YourCondition = MechanicalCondition.Good,
                TheirCondition = MechanicalCondition.Good
            };

        TradeDecisionResult result =
            service.Analyze(input);

        Assert.Equal(
            "NEGOTIATE",
            result.Recommendation);
    }

    [Fact]
    public void Analyze_HighRiskTrade_CannotReturnPonyUp()
    {
        var service =
            new TradeScoringService();

        var input =
            new TradeDecisionInput
            {
                YourValue = 4000m,
                TheirValue = 8000m,
                YourCondition = MechanicalCondition.Poor,
                TheirCondition = MechanicalCondition.Excellent,
                TheirTitleStatus = TitleStatus.Flood,
                TheirAccidentHistory = AccidentHistory.Major,
                TheirRuns = false,
                TheirDrives = false
            };

        TradeDecisionResult result =
            service.Analyze(input);

        Assert.NotEqual(
            "PONY UP",
            result.Recommendation);
    }
}
