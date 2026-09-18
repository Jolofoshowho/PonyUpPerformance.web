using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models;

public class RepairDecisionInput
{
    [Display(Name = "VIN")]
    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? Vin { get; set; } = string.Empty;

    [Display(Name = "Year")]
    [Range(
        1886,
        2100,
        ErrorMessage = "Enter a valid vehicle year.")]
    public int? VehicleYear { get; set; }

    [Display(Name = "Make")]
    [StringLength(50)]
    public string? VehicleMake { get; set; } =
        string.Empty;

    [Display(Name = "Model")]
    [StringLength(80)]
    public string? VehicleModel { get; set; } =
        string.Empty;

    [Display(Name = "Mileage")]
    [Range(
        0,
        2_000_000,
        ErrorMessage = "Enter valid mileage.")]
    public int? Mileage { get; set; }

    [Display(Name = "Estimated Vehicle Value")]
    [Range(
        typeof(decimal),
        "0.01",
        "100000000",
        ErrorMessage = "Enter a valid vehicle value.")]
    public decimal? VehicleValue { get; set; }

    [Display(Name = "Repair Cost")]
    [Range(
        typeof(decimal),
        "0",
        "100000000",
        ErrorMessage = "Enter a valid repair cost.")]
    public decimal? RepairCost { get; set; }

    [Display(Name = "Overall Vehicle Condition")]
    public MechanicalCondition Condition { get; set; }
        = MechanicalCondition.NotProvided;

    [Display(Name = "Safety-Critical Repair")]
    public bool? IsSafetyCritical { get; set; }

    [Display(Name = "Years You Plan to Keep It")]
    [Range(
        0,
        100,
        ErrorMessage = "Enter a valid ownership period.")]
    public int? OwnershipYears { get; set; }
}
