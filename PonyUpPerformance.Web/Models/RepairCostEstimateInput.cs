namespace PonyUpPerformance.Web.Models
{
    public class RepairCostEstimateInput
    {
        public string City { get; set; } = "";

        public string State { get; set; } = "";

        public string ZipCode { get; set; } = "";

        public int VehicleYear { get; set; }

        public string VehicleMake { get; set; } = "";

        public string VehicleModel { get; set; } = "";

        public string RepairType { get; set; } = "";

        public bool IncludeDiagnosticFee { get; set; }

        public bool IncludeTowFee { get; set; }
    }
}