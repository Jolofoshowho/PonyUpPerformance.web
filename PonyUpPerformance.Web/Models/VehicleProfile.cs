namespace PonyUpPerformance.Web.Models
{
    public class VehicleProfile
    {
        public string Vin { get; set; } = "";

        public int? Year { get; set; }

        public string Make { get; set; } = "";

        public string Model { get; set; } = "";

        public string Trim { get; set; } = "";

        public string Series { get; set; } = "";

        public string Engine { get; set; } = "";

        public string EngineDisplacement { get; set; } = "";

        public string EngineCylinders { get; set; } = "";

        public string Transmission { get; set; } = "";

        public string TransmissionStyle { get; set; } = "";

        public string Drivetrain { get; set; } = "";

        public string BodyStyle { get; set; } = "";

        public string VehicleType { get; set; } = "";

        public string FuelType { get; set; } = "";

        public string Manufacturer { get; set; } = "";

        public string PlantCountry { get; set; } = "";

        public string PlantState { get; set; } = "";

        public string PlantCity { get; set; } = "";

        public decimal? BaseMsrp { get; set; }

        public string ReliabilityTrend { get; set; } = "";

        public int? ReliabilityScore { get; set; }

        public string RecallStatus { get; set; } = "";

        public int OpenRecallCount { get; set; }

        public decimal? EstimatedMarketValue { get; set; }

        public int? ExpectedAnnualMileage { get; set; }

        public string SafetyRating { get; set; } = "";

        public string AftermarketSupport { get; set; } = "";

        public List<string> KnownIssues { get; set; } = new();

        public List<string> OpenRecalls { get; set; } = new();

        public DateTime? DecodedAtUtc { get; set; }

        public string DecodeSource { get; set; } = "";

        public bool DecodeSuccessful { get; set; }

        public List<string> DecodeWarnings { get; set; } = new();

        public string DisplayName
        {
            get
            {
                return string.Join(
                    " ",
                    new[]
                    {
                        Year?.ToString(),
                        Make,
                        Model,
                        Trim
                    }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            }
        }
    }
}
