namespace PonyUpPerformance.Web.Models;

public class SellDecisionInput
{
    public int VehicleYear { get; set; }
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal UpcomingRepairCost { get; set; }
    public VehicleCondition Condition { get; set; }
    public bool NeedReplacementVehicle { get; set; }
}