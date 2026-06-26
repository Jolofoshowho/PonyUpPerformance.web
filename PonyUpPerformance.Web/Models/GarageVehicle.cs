using System;

namespace PonyUpPerformance.Web.Models
{
    public class GarageVehicle
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        public string Vin { get; set; } = "";

        public int Year { get; set; }

        public string Make { get; set; } = "";

        public string Model { get; set; } = "";

        public string Trim { get; set; } = "";

        public int Mileage { get; set; }

        public string BodyStyle { get; set; } = "Sedan";

        public string RenderType { get; set; } = "Sedan";

        public string Drivetrain { get; set; } = "";

        public string FuelType { get; set; } = "Gasoline";

        public string Engine { get; set; } = "";

        public string SelectedPaintName { get; set; } = "";

        public string SelectedPaintCode { get; set; } = "";

        public string SelectedPaintHex { get; set; } = "#b8b8b8";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}