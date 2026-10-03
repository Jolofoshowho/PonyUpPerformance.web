using System.ComponentModel.DataAnnotations;
using PonyUpPerformance.Web.Models;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class TradeInputValidationTests
{
    [Fact]
    public void EmptyTradeInput_IsValid()
    {
        var input =
            new TradeDecisionInput();

        List<ValidationResult> results =
            Validate(input);

        Assert.Empty(results);
    }

    [Fact]
    public void NegativeTradeEvidence_IsRejected()
    {
        var input =
            new TradeDecisionInput
            {
                YourYear = -1,
                YourValue = -100m,
                YourMileage = -1,
                YourEstimatedRepairCost = -1m,
                CashYouAdd = -1m,
                TheirYear = -1,
                TheirValue = -100m,
                TheirMileage = -1,
                TheirEstimatedRepairCost = -1m,
                CashTheyAdd = -1m
            };

        List<ValidationResult> results =
            Validate(input);

        Assert.NotEmpty(results);
        Assert.True(
            results.Count >= 10,
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
