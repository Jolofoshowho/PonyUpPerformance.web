using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models;

public class RepairDecisionInput
{
    [Display(Name = "VIN")]
    [RegularExpression(
        @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
        ErrorMessage = "Enter a valid 17-character VIN.")]
    public string? Vin { get; set; } = string.Empty;

    public int VehicleYear { get; set; }

    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public int Mileage { get; set; }

    public decimal VehicleValue { get; set; }

    public decimal RepairCost { get; set; }

    public VehicleCondition Condition { get; set; }

    public bool IsSafetyCritical { get; set; }

    public int OwnershipYears { get; set; }
}
