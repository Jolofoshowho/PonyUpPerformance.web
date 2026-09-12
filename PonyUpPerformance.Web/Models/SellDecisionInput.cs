using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models;

public class SellDecisionInput
{
    [Display(Name = "VIN")]
    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? Vin { get; set; } = string.Empty;

    [Range(
        1886,
        2100,
        ErrorMessage = "Enter a valid vehicle year.")]
    public int? Year { get; set; }

    [StringLength(50)]
    public string? Make { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Model { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Trim { get; set; } = string.Empty;

    [Range(
        0,
        2_000_000,
        ErrorMessage = "Enter valid mileage.")]
    public int? Mileage { get; set; }

    [Display(Name = "Current Offer / Expected As-Is Sale Price")]
    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid sale price.")]
    public decimal? ExpectedSalePrice { get; set; }

    [Display(Name = "Estimated Market Value")]
    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid market value.")]
    public decimal? MarketValue { get; set; }

    [Display(Name = "Estimated Repair Cost Before Selling")]
    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid repair cost.")]
    public decimal? EstimatedRepairCost { get; set; }

    [Display(Name = "Mechanical Condition")]
    public MechanicalCondition MechanicalCondition { get; set; }
        = MechanicalCondition.NotProvided;

    [Display(Name = "Title Status")]
    public TitleStatus TitleStatus { get; set; }
        = TitleStatus.NotProvided;

    [Display(Name = "Accident History")]
    public AccidentHistory AccidentHistory { get; set; }
        = AccidentHistory.NotProvided;
}
