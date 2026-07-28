using System.ComponentModel.DataAnnotations;

namespace PonyUpPerformance.Web.Models
{
    public class BuyDecisionInput
    {
        [Display(Name = "VIN")]
        [RegularExpression(
            @"^$|(?i)^[A-HJ-NPR-Z0-9]{17}$",
            ErrorMessage = "Enter a valid 17-character VIN.")]
        public string Vin { get; set; } = string.Empty;

        [Required]
        [Range(1886, 2100, ErrorMessage = "Enter a valid vehicle year.")]
        public int Year { get; set; }

        [Required]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Model { get; set; } = string.Empty;

        [StringLength(80)]
        public string Trim { get; set; } = string.Empty;

        [StringLength(120)]
        public string Engine { get; set; } = string.Empty;

        [StringLength(80)]
        public string Transmission { get; set; } = string.Empty;

        [StringLength(80)]
        public string Drivetrain { get; set; } = string.Empty;

        [Display(Name = "Body Style")]
        [StringLength(80)]
        public string BodyStyle { get; set; } = string.Empty;

        [Display(Name = "Fuel Type")]
        [StringLength(50)]
        public string FuelType { get; set; } = string.Empty;

        [Required]
        [Range(0, 2_000_000, ErrorMessage = "Enter valid mileage.")]
        public int Mileage { get; set; }

        [Required]
        [Display(Name = "Asking Price")]
        [Range(typeof(decimal), "0.01", "100000000")]
        public decimal AskingPrice { get; set; }

        [Required]
        [Display(Name = "Estimated Repair Cost")]
        [Range(typeof(decimal), "0", "100000000")]
        public decimal EstimatedRepairCost { get; set; }

        [Required]
        [Display(Name = "Estimated Market Value")]
        [Range(typeof(decimal), "0.01", "100000000")]
        public decimal MarketValue { get; set; }

        [Required]
        [Display(Name = "Mechanical Condition")]
        public MechanicalCondition MechanicalCondition { get; set; }
            = MechanicalCondition.NotProvided;

        [Required]
        [Display(Name = "Title Status")]
        public TitleStatus TitleStatus { get; set; }
            = TitleStatus.NotProvided;

        [Required]
        [Display(Name = "Accident History")]
        public AccidentHistory AccidentHistory { get; set; }
            = AccidentHistory.NotProvided;

        [Required]
        [Display(Name = "Intended Use")]
        public IntendedUse IntendedUse { get; set; }
            = IntendedUse.NotProvided;
    }

    public enum IntendedUse
    {
        NotProvided = -1,
        DailyDriver = 0,
        WorkVehicle = 1,
        FamilyVehicle = 2,
        ProjectVehicle = 3,
        PerformanceBuild = 4
    }
}
