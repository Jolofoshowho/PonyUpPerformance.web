namespace PonyUpPerformance.Web.Models;

public class RepairDecisionInput
{
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
