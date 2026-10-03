using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models;

public class TradeDecisionInput
{
    // =====================================================
    // YOUR VEHICLE
    // =====================================================

    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? YourVin { get; set; } = string.Empty;

    [Range(
        1886,
        2100,
        ErrorMessage = "Enter a valid vehicle year.")]
    public int? YourYear { get; set; }

    [StringLength(50)]
    public string? YourMake { get; set; } = string.Empty;

    [StringLength(80)]
    public string? YourModel { get; set; } = string.Empty;

    [StringLength(80)]
    public string? YourTrim { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid market value.")]
    public decimal? YourValue { get; set; }

    [Range(
        0,
        2_000_000,
        ErrorMessage = "Enter valid mileage.")]
    public int? YourMileage { get; set; }

    [StringLength(120)]
    public string? YourEngine { get; set; } = string.Empty;

    [Range(
        0,
        3000,
        ErrorMessage = "Enter valid horsepower.")]
    public int? YourHorsepower { get; set; }

    [Range(
        0,
        5000,
        ErrorMessage = "Enter valid torque.")]
    public int? YourTorqueLbFt { get; set; }

    [StringLength(80)]
    public string? YourTransmission { get; set; } = string.Empty;

    [StringLength(80)]
    public string? YourDrivetrain { get; set; } = string.Empty;

    [StringLength(50)]
    public string? YourFuelType { get; set; } = string.Empty;

    [StringLength(80)]
    public string? YourBodyStyle { get; set; } = string.Empty;

    [Range(
        0,
        500,
        ErrorMessage = "Enter valid city MPG.")]
    public int? YourCityMpg { get; set; }

    [Range(
        0,
        500,
        ErrorMessage = "Enter valid highway MPG.")]
    public int? YourHighwayMpg { get; set; }

    [Range(
        0,
        100_000,
        ErrorMessage = "Enter a valid curb weight.")]
    public int? YourCurbWeightLbs { get; set; }

    public MechanicalCondition YourCondition { get; set; }
        = MechanicalCondition.NotProvided;

    public TitleStatus YourTitleStatus { get; set; }
        = TitleStatus.NotProvided;

    public AccidentHistory YourAccidentHistory { get; set; }
        = AccidentHistory.NotProvided;

    public bool? YourRuns { get; set; }

    public bool? YourDrives { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid repair cost.")]
    public decimal? YourEstimatedRepairCost { get; set; }


    // =====================================================
    // THEIR VEHICLE
    // =====================================================

    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? TheirVin { get; set; } = string.Empty;

    [Range(
        1886,
        2100,
        ErrorMessage = "Enter a valid vehicle year.")]
    public int? TheirYear { get; set; }

    [StringLength(50)]
    public string? TheirMake { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TheirModel { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TheirTrim { get; set; } = string.Empty;

    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid market value.")]
    public decimal? TheirValue { get; set; }

    [Range(
        0,
        2_000_000,
        ErrorMessage = "Enter valid mileage.")]
    public int? TheirMileage { get; set; }

    [StringLength(120)]
    public string? TheirEngine { get; set; } = string.Empty;

    [Range(
        0,
        3000,
        ErrorMessage = "Enter valid horsepower.")]
    public int? TheirHorsepower { get; set; }

    [Range(
        0,
        5000,
        ErrorMessage = "Enter valid torque.")]
    public int? TheirTorqueLbFt { get; set; }

    [StringLength(80)]
    public string? TheirTransmission { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TheirDrivetrain { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TheirFuelType { get; set; } = string.Empty;

    [StringLength(80)]
    public string? TheirBodyStyle { get; set; } = string.Empty;

    [Range(
        0,
        500,
        ErrorMessage = "Enter valid city MPG.")]
    public int? TheirCityMpg { get; set; }

    [Range(
        0,
        500,
        ErrorMessage = "Enter valid highway MPG.")]
    public int? TheirHighwayMpg { get; set; }

    [Range(
        0,
        100_000,
        ErrorMessage = "Enter a valid curb weight.")]
    public int? TheirCurbWeightLbs { get; set; }

    public MechanicalCondition TheirCondition { get; set; }
        = MechanicalCondition.NotProvided;

    public TitleStatus TheirTitleStatus { get; set; }
        = TitleStatus.NotProvided;

    public AccidentHistory TheirAccidentHistory { get; set; }
        = AccidentHistory.NotProvided;

    public bool? TheirRuns { get; set; }

    public bool? TheirDrives { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid repair cost.")]
    public decimal? TheirEstimatedRepairCost { get; set; }


    // =====================================================
    // TRADE DEAL
    // =====================================================

    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid cash amount.")]
    public decimal? CashYouAdd { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid cash amount.")]
    public decimal? CashTheyAdd { get; set; }
}
