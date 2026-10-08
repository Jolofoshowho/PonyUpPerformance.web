namespace PonyUpPerformance.Web.Models;

public sealed class BetaAccessRedemption
{
    public int Id { get; set; }

    public int BetaAccessCodeId { get; set; }

    public string UserId { get; set; } =
        string.Empty;

    public int CreditsGranted { get; set; }

    public DateTime RedeemedOn { get; set; } =
        DateTime.UtcNow;
}
