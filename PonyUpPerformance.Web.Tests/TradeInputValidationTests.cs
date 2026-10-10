using System.ComponentModel.DataAnnotations;
using PonyUpPerformance.Web.Models;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class TradeInputValidationTests
{
    [Fact]
    public void MissingMarketValues_IsRejected()
    {
        var input =
            new TradeDecisionInput();

        List<ValidationResult> results =
            Validate(input);

        Assert.Contains(
            results,
            x => x.MemberNames.Contains(
                nameof(TradeDecisionInput.YourValue)));

        Assert.Contains(
            results,
            x => x.MemberNames.Contains(
                nameof(TradeDecisionInput.TheirValue)));
    }

    [Fact]
    public void OnlyBothMarketValues_IsValid()
    {
        var input =
            new TradeDecisionInput
            {
                YourValue = 5000m,
                TheirValue = 7500m
            };

        List<ValidationResult> results =
            Validate(input);

        Assert.Empty(results);
    }

    [Fact]
    public void NegativeOptionalTradeEvidence_IsRejectedWhenSupplied()
    {
        var input =
            new TradeDecisionInput
            {
                YourValue = 5000m,
                TheirValue = 7500m,
                YourYear = -1,
                YourMileage = -1,
                YourEstimatedRepairCost = -1m,
                CashYouAdd = -1m,
                TheirYear = -1,
                TheirMileage = -1,
                TheirEstimatedRepairCost = -1m,
                CashTheyAdd = -1m
            };

        List<ValidationResult> results =
            Validate(input);

        Assert.NotEmpty(results);
        Assert.True(
            results.Count >= 8,
            $"Expected invalid supplied values to be rejected, but only {results.Count} validation errors were returned.");
    }

    private static List<ValidationResult> Validate(
        TradeDecisionInput input)
    {
        var results =
            new List<ValidationResult>();

        Validator.TryValidateObject(
            input,
            new ValidationContext(input),
            results,
            validateAllProperties: true);

        return results;
    }
}
