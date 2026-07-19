namespace PonyUpPerformance.Web.Models
{
    public enum TitleStatus
    {
        Clean,
        Rebuilt,
        Salvage,
        Flood
    }

    public enum AccidentHistory
    {
        None,
        Minor,
        Moderate,
        Major
    }

    public enum MaintenanceHistory
    {
        Complete,
        Partial,
        Unknown,
        Neglected
    }

    public enum RecallStatus
    {
        None,
        Open,
        MajorSafety
    }

    public enum LocalMarketAvailability
    {
        VeryRare,
        Rare,
        Average,
        Common,
        VeryCommon
    }

    public class BuyDecisionInput
    {
        public int Year { get; set; }

        public string Make { get; set; } = "";

        public string Model { get; set; } = "";

        public int Mileage { get; set; }

        public decimal AskingPrice { get; set; }

        public decimal EstimatedRepairCost { get; set; }

        public decimal MarketValue { get; set; }

        public TitleStatus TitleStatus { get; set; }

        public AccidentHistory AccidentHistory { get; set; }

        public MaintenanceHistory MaintenanceHistory { get; set; }

        public RecallStatus RecallStatus { get; set; }

        public LocalMarketAvailability LocalMarketAvailability { get; set; }
    }
}
