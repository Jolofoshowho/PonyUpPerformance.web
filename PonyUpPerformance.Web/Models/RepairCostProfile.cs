namespace PonyUpPerformance.Web.Models
{
    public class RepairCostProfile
    {
        public string RepairType { get; set; } = "";

        public decimal LaborHoursLow { get; set; }
        public decimal LaborHoursAverage { get; set; }
        public decimal LaborHoursHigh { get; set; }

        public decimal PartsLow { get; set; }
        public decimal PartsAverage { get; set; }
        public decimal PartsHigh { get; set; }
    }
}