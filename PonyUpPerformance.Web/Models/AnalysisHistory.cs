using System;

namespace PonyUpPerformance.Web.Models
{
    public class AnalysisHistory
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        public string AnalysisType { get; set; } = "Repair";

        public int VehicleYear { get; set; }

        public string VehicleMake { get; set; } = "";

        public string VehicleModel { get; set; } = "";

        public int Mileage { get; set; }

        public string RepairType { get; set; } = "";

        public decimal LowEstimate { get; set; }

        public decimal ExpectedEstimate { get; set; }

        public decimal HighEstimate { get; set; }

        public decimal VehicleValue { get; set; }

        public string VehicleCondition { get; set; } = "";

        public int PlannedOwnershipYears { get; set; }

        public string Recommendation { get; set; } = "";

        public int ConfidenceScore { get; set; }

        public string RiskLevel { get; set; } = "";

        public string FinancialImpact { get; set; } = "";

        public string Reasoning { get; set; } = "";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}