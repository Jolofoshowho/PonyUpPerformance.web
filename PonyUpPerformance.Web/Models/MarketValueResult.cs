namespace PonyUpPerformance.Web.Models
{
    public class MarketValueResult
    {
        public decimal EstimatedMarketValue { get; set; }

        public decimal LowRetailValue { get; set; }

        public decimal HighRetailValue { get; set; }

        public decimal TradeInValue { get; set; }

        public decimal WholesaleValue { get; set; }

        public decimal PrivatePartyValue { get; set; }

        public decimal EstimatedAuctionValue { get; set; }

        public string MarketStrength { get; set; } = "";

        public int DaysOnMarket { get; set; }

        public string Summary { get; set; } = "";

        public List<string> Sources { get; set; } = new();
    }
}
