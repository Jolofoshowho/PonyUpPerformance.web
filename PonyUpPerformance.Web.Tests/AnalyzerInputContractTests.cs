using System.ComponentModel.DataAnnotations;
using PonyUpPerformance.Web.Models;
using Xunit;

namespace PonyUpPerformance.Web.Tests;

public sealed class AnalyzerInputContractTests
{
    [Fact]
    public void BlankOptionalAnalyzerInputs_AreValid()
    {
        object[] inputs =
        {
            new RepairDecisionInput(),
            new BuyDecisionInput(),
            new SellDecisionInput(),
            new UpgradeDecisionInput()
        };

        foreach (object input in inputs)
        {
            var results =
                new List<ValidationResult>();

            bool valid =
                Validator.TryValidateObject(
                    input,
                    new ValidationContext(input),
                    results,
                    validateAllProperties: true);

            Assert.True(
                valid,
                $"{input.GetType().Name} should allow blank optional evidence. " +
                string.Join(
                    " | ",
                    results.Select(x => x.ErrorMessage)));
        }
    }

    [Fact]
    public void Trade_WithOnlyBothMarketValues_IsValid()
    {
        var input =
            new TradeDecisionInput
            {
                YourValue = 5000m,
                TheirValue = 7500m
            };

        var results =
            new List<ValidationResult>();

        bool valid =
            Validator.TryValidateObject(
                input,
                new ValidationContext(input),
                results,
                validateAllProperties: true);

        Assert.True(
            valid,
            string.Join(
                " | ",
                results.Select(x => x.ErrorMessage)));
    }
}
