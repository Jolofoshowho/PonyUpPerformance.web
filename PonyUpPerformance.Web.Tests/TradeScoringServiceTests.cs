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
}
