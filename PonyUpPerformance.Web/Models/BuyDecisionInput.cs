namespace PonyUpPerformance.Web.Models
{
    public class BuyDecisionInput
    {
        public string Vin { get; set; } = "";

        public int VehicleYear { get; set; }

        public string VehicleMake { get; set; } = "";

        public string VehicleModel { get; set; } = "";

        public string VehicleTrim { get; set; } = "";

        public int Mileage { get; set; }

        public decimal AskingPrice { get; set; }

        public decimal EstimatedMarketValue { get; set; }

        public string SellerType { get; set; } = "Private Party";

        public string AccidentHistory { get; set; } = "None";

        public string TitleStatus { get; set; } = "Clean";

        public string MaintenanceHistory { get; set; } = "Unknown";

        public string ReliabilityConcernLevel { get; set; } = "Moderate";

        public string RecallStatus { get; set; } = "Unknown";

        public string LocalMarketAvailability { get; set; } = "Normal";

        public string Notes { get; set; } = "";
    }
}