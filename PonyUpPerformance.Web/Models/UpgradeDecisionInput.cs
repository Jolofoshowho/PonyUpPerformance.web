using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models;

public class UpgradeDecisionInput
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

    [Display(Name = "Current Horsepower")]
    [Range(
        0,
        3000,
        ErrorMessage = "Enter valid horsepower.")]
    public int? CurrentHorsepower { get; set; }

    [Display(Name = "Current Vehicle Value")]
    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid vehicle value.")]
    public decimal? CurrentValue { get; set; }

    [Display(Name = "Upgrade Cost")]
    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid upgrade cost.")]
    public decimal? UpgradeCost { get; set; }

    [Display(Name = "Estimated Resale Value Added")]
    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid estimated value increase.")]
    public decimal? ValueAdded { get; set; }

    [Display(Name = "Estimated Horsepower Gain")]
    [Range(
        0,
        3000,
        ErrorMessage = "Enter a valid horsepower gain.")]
    public int? HorsepowerGain { get; set; }

    [Display(Name = "Upgrade Goal")]
    public UpgradeGoal Goal { get; set; }
        = UpgradeGoal.NotProvided;

    [Display(Name = "Reliability Impact")]
    public UpgradeReliabilityImpact ReliabilityImpact { get; set; }
        = UpgradeReliabilityImpact.NotProvided;

    [Display(Name = "Daily Driver")]
    public bool? IsDailyDriver { get; set; }
}

public enum UpgradeGoal
{
    NotProvided = -1,
    Performance = 0,
    Reliability = 1,
    Appearance = 2,
    ComfortTechnology = 3,
    UtilityTowing = 4,
    FuelEconomy = 5,
    Restoration = 6,
    ResaleValue = 7
}

public enum UpgradeReliabilityImpact
{
    NotProvided = -1,
    MuchBetter = 0,
    Better = 1,
    NoChange = 2,
    Worse = 3,
    MuchWorse = 4
}
