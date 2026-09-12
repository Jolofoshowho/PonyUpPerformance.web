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

    public int? YourYear { get; set; }

    public string? YourMake { get; set; } = string.Empty;

    public string? YourModel { get; set; } = string.Empty;

    public string? YourTrim { get; set; } = string.Empty;

    public decimal? YourValue { get; set; }

    public int? YourMileage { get; set; }

    public string? YourEngine { get; set; } = string.Empty;

    public int? YourHorsepower { get; set; }

    public int? YourTorqueLbFt { get; set; }

    public string? YourTransmission { get; set; } = string.Empty;

    public string? YourDrivetrain { get; set; } = string.Empty;

    public string? YourFuelType { get; set; } = string.Empty;

    public string? YourBodyStyle { get; set; } = string.Empty;

    public int? YourCityMpg { get; set; }

    public int? YourHighwayMpg { get; set; }

    public int? YourCurbWeightLbs { get; set; }

    public MechanicalCondition YourCondition { get; set; }
        = MechanicalCondition.NotProvided;

    public TitleStatus YourTitleStatus { get; set; }
        = TitleStatus.NotProvided;

    public AccidentHistory YourAccidentHistory { get; set; }
        = AccidentHistory.NotProvided;

    public bool? YourRuns { get; set; }

    public bool? YourDrives { get; set; }

    public decimal? YourEstimatedRepairCost { get; set; }


    // =====================================================
    // THEIR VEHICLE
    // =====================================================

    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? TheirVin { get; set; } = string.Empty;

    public int? TheirYear { get; set; }

    public string? TheirMake { get; set; } = string.Empty;

    public string? TheirModel { get; set; } = string.Empty;

    public string? TheirTrim { get; set; } = string.Empty;

    public decimal? TheirValue { get; set; }

    public int? TheirMileage { get; set; }

    public string? TheirEngine { get; set; } = string.Empty;

    public int? TheirHorsepower { get; set; }

    public int? TheirTorqueLbFt { get; set; }

    public string? TheirTransmission { get; set; } = string.Empty;

    public string? TheirDrivetrain { get; set; } = string.Empty;

    public string? TheirFuelType { get; set; } = string.Empty;

    public string? TheirBodyStyle { get; set; } = string.Empty;

    public int? TheirCityMpg { get; set; }

    public int? TheirHighwayMpg { get; set; }

    public int? TheirCurbWeightLbs { get; set; }

    public MechanicalCondition TheirCondition { get; set; }
        = MechanicalCondition.NotProvided;

    public TitleStatus TheirTitleStatus { get; set; }
        = TitleStatus.NotProvided;

    public AccidentHistory TheirAccidentHistory { get; set; }
        = AccidentHistory.NotProvided;

    public bool? TheirRuns { get; set; }

    public bool? TheirDrives { get; set; }

    public decimal? TheirEstimatedRepairCost { get; set; }


    // =====================================================
    // TRADE DEAL
    // =====================================================

    public decimal? CashYouAdd { get; set; }

    public decimal? CashTheyAdd { get; set; }
}
