namespace PonyUpPerformance.Web.Models
{
    public class LocationRateProfile
    {
        public string State { get; set; } = "";
        public string Region { get; set; } = "";

        public decimal LaborRateLow { get; set; }
        public decimal LaborRateAverage { get; set; }
        public decimal LaborRateHigh { get; set; }

        public decimal SalesTaxRate { get; set; }

        public decimal ShopFeeAverage { get; set; }
    }
}