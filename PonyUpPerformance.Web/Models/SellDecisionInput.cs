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

    [Display(Name = "Asking / Expected Sale Price")]
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

    [Display(Name = "Overall Vehicle Condition")]
    public SellCondition Condition { get; set; }
        = SellCondition.NotProvided;

    [Display(Name = "Title Status")]
    public TitleStatus TitleStatus { get; set; }
        = TitleStatus.NotProvided;

    [Display(Name = "Accident History")]
    public AccidentHistory AccidentHistory { get; set; }
        = AccidentHistory.NotProvided;

    [Display(Name = "Runs")]
    public bool? Runs { get; set; }

    [Display(Name = "Drives")]
    public bool? Drives { get; set; }

    [Display(Name = "Intended Use")]
    public SellIntendedUse IntendedUse { get; set; }
        = SellIntendedUse.NotProvided;
}

public enum SellCondition
{
    NotProvided = -1,
    Excellent = 0,
    Good = 1,
    Fair = 2,
    Poor = 3,
    Severe = 4
}

public enum SellIntendedUse
{
    NotProvided = -1,
    DailyDriver = 0,
    WorkVehicle = 1,
    FamilyVehicle = 2,
    ProjectVehicle = 3,
    PerformanceBuild = 4,
    Restoration = 5
}
