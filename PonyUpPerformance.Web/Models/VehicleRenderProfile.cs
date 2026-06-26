namespace PonyUpPerformance.Web.Models
{
    public class VehicleRenderProfile
    {
        public string BodyStyle { get; set; } = "Sedan";

        public string RenderType { get; set; } = "Sedan";

        public int Length { get; set; } = 260;

        public int Height { get; set; } = 92;

        public int HoodLength { get; set; } = 70;

        public int CabinLength { get; set; } = 105;

        public int DeckLength { get; set; } = 55;

        public int RoofHeight { get; set; } = 42;

        public int WheelBase { get; set; } = 175;

        public int GroundClearance { get; set; } = 18;

        public bool HasTruckBed { get; set; }

        public bool IsSuv { get; set; }

        public bool IsCoupe { get; set; }

        public bool IsMuscleCar { get; set; }
    }
}