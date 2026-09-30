namespace PonyUpPerformance.Web.Models;

public class RevUpReport
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Vin { get; set; } = string.Empty;

    public int? VehicleYear { get; set; }

    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderReportId { get; set; } = string.Empty;

    public int? ExternalCostCents { get; set; }

    public string ReportJson { get; set; } = "{}";

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedOn { get; set; }
}
