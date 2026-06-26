namespace PonyUpPerformance.Web.Models
{
    public class NhtsaVinDecodeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Vin { get; set; } = "";
        public int Year { get; set; }
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string Trim { get; set; } = "";
        public string BodyStyle { get; set; } = "";
        public string Drivetrain { get; set; } = "";
        public string FuelType { get; set; } = "";
        public string Engine { get; set; } = "";
    }
}