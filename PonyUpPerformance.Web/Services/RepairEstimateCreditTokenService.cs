using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace PonyUpPerformance.Web.Services;

public sealed class RepairEstimateCreditTokenService
{
    private readonly ITimeLimitedDataProtector _protector;

    public RepairEstimateCreditTokenService(
        IDataProtectionProvider dataProtectionProvider)
    {
        _protector =
            dataProtectionProvider
                .CreateProtector(
                    "PonyUp.Repair.EstimateCredit.v1")
                .ToTimeLimitedDataProtector();
    }

    public string Issue(
        string userId)
    {
        if (string.IsNullOrWhiteSpace(
                userId))
        {
            throw new ArgumentException(
                "A user ID is required.",
                nameof(userId));
        }

        return _protector.Protect(
            userId,
            TimeSpan.FromMinutes(30));
    }

    public bool IsValid(
        string? token,
        string? userId)
    {
        if (string.IsNullOrWhiteSpace(
                token) ||
            string.IsNullOrWhiteSpace(
                userId))
        {
            return false;
        }

        try
        {
            string payload =
                _protector.Unprotect(
                    token);

            return string.Equals(
                payload,
                userId,
                StringComparison.Ordinal);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
