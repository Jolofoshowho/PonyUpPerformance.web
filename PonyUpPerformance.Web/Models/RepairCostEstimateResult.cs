namespace PonyUpPerformance.Web.Models
{
    public class RepairCostEstimateResult
    {
        public string RepairType { get; set; } = "";

        public string LocationUsed { get; set; } = "";

        public decimal LaborRateLow { get; set; }

        public decimal LaborRateAverage { get; set; }

        public decimal LaborRateHigh { get; set; }

        public decimal LaborHoursLow { get; set; }

        public decimal LaborHoursAverage { get; set; }

        public decimal LaborHoursHigh { get; set; }

        public decimal PartsLow { get; set; }

        public decimal PartsAverage { get; set; }

        public decimal PartsHigh { get; set; }

        public decimal ShopFeeAverage { get; set; }

        public decimal SalesTaxRate { get; set; }

        public decimal DiagnosticFee { get; set; }

        public decimal TowFee { get; set; }

        public decimal LowEstimate { get; set; }

        public decimal ExpectedEstimate { get; set; }

        public decimal HighEstimate { get; set; }

        public string EstimateNote { get; set; } = "";
    }
}