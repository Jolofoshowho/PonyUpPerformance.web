namespace PonyUpPerformance.Web.Models;

public class TradeDecisionInput
{
	public int VehicleYear { get; set; }
	public string VehicleMake { get; set; } = string.Empty;
	public string VehicleModel { get; set; } = string.Empty;
	public int Mileage { get; set; }
	public decimal TradeOffer { get; set; }
	public decimal PrivateSaleValue { get; set; }
	public decimal RepairNeeds { get; set; }
	public decimal ReplacementVehicleCost { get; set; }
}