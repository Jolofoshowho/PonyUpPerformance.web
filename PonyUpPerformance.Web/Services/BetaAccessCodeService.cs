using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;

namespace PonyUpPerformance.Web.Services;

public enum BetaAccessRedeemStatus
{
    Applied,
    AlreadyRedeemed,
    InvalidCode,
    Exhausted,
    Expired
}

public sealed record BetaAccessRedeemResult(
    BetaAccessRedeemStatus Status,
    int CreditsGranted,
    int RemainingCredits,
    string Message);

public sealed class BetaAccessCodeService
{
    private readonly ApplicationDbContext _dbContext;

    public BetaAccessCodeService(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BetaAccessRedeemResult> RedeemAsync(
        ApplicationUser user,
        string? rawCode)
    {
        string normalized =
            NormalizeCode(rawCode);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Result(
                BetaAccessRedeemStatus.InvalidCode,
                user,
                0,
                "Enter a valid beta access code.");
        }

        string codeHash =
            HashCode(normalized);

        await using var transaction =
            _dbContext.Database.IsRelational()
                ? await _dbContext.Database.BeginTransactionAsync()
                : null;

        BetaAccessCode? code;

        if (_dbContext.Database.ProviderName?
                .Contains(
                    "Npgsql",
                    StringComparison.OrdinalIgnoreCase) == true)
        {
            code =
                await _dbContext.BetaAccessCodes
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM "BetaAccessCodes"
                        WHERE "CodeHash" = {codeHash}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync();
        }
        else
        {
            code =
                await _dbContext.BetaAccessCodes
                    .SingleOrDefaultAsync(
                        x => x.CodeHash == codeHash);
        }

        if (code == null ||
            !code.IsActive)
        {
            return Result(
                BetaAccessRedeemStatus.InvalidCode,
                user,
                0,
                "That beta access code is not valid.");
        }

        if (code.ExpiresOn.HasValue &&
            code.ExpiresOn.Value <= DateTime.UtcNow)
        {
            return Result(
                BetaAccessRedeemStatus.Expired,
                user,
                0,
                "That beta access code has expired.");
        }

        bool alreadyRedeemed =
            await _dbContext.BetaAccessRedemptions
                .AnyAsync(
                    x =>
                        x.BetaAccessCodeId == code.Id &&
                        x.UserId == user.Id);

        if (alreadyRedeemed)
        {
            return Result(
                BetaAccessRedeemStatus.AlreadyRedeemed,
                user,
                0,
                "This account has already redeemed that beta code.");
        }

        if (code.RedemptionCount >=
            code.MaxRedemptions)
        {
            return Result(
                BetaAccessRedeemStatus.Exhausted,
                user,
                0,
                "That beta access code has reached its redemption limit.");
        }

        int credits =
            Math.Max(
                0,
                code.CreditsGranted);

        user.RemainingCredits +=
            credits;

        code.RedemptionCount += 1;

        _dbContext.BetaAccessRedemptions.Add(
            new BetaAccessRedemption
            {
                BetaAccessCodeId =
                    code.Id,

                UserId =
                    user.Id,

                CreditsGranted =
                    credits,

                RedeemedOn =
                    DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync();

        if (transaction != null)
        {
            await transaction.CommitAsync();
        }

        return Result(
            BetaAccessRedeemStatus.Applied,
            user,
            credits,
            $"Beta access unlocked. {credits} analysis credits were added to your account.");
    }

    public static string NormalizeCode(
        string? rawCode)
    {
        return (rawCode ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }

    public static string HashCode(
        string normalizedCode)
    {
        byte[] bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    normalizedCode));

        return Convert.ToHexString(
            bytes);
    }

    private static BetaAccessRedeemResult Result(
        BetaAccessRedeemStatus status,
        ApplicationUser user,
        int creditsGranted,
        string message)
    {
        return new BetaAccessRedeemResult(
            status,
            creditsGranted,
            Math.Max(
                0,
                user.RemainingCredits),
            message);
    }
}
