namespace PonyUpPerformance.Web.Models;

public sealed class BetaAccessCode
{
    public int Id { get; set; }

    public string CodeHash { get; set; } =
        string.Empty;

    public int CreditsGranted { get; set; } = 5;

    public int MaxRedemptions { get; set; } = 10;

    public int RedemptionCount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresOn { get; set; }

    public DateTime CreatedOn { get; set; } =
        DateTime.UtcNow;
}
