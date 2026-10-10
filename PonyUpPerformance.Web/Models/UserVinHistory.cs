namespace PonyUpPerformance.Web.Models;

public sealed class UserVinHistory
{
    public int Id { get; set; }

    public string UserId { get; set; } =
        string.Empty;

    public string Vin { get; set; } =
        string.Empty;

    public int? Year { get; set; }

    public string Make { get; set; } =
        string.Empty;

    public string Model { get; set; } =
        string.Empty;

    public DateTime LastUsedOn { get; set; } =
        DateTime.UtcNow;
}
