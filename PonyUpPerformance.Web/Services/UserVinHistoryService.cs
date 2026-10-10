using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using System.Security.Claims;

namespace PonyUpPerformance.Web.Services;

public sealed class UserVinHistoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserVinHistoryService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task SaveAsync(
        ClaimsPrincipal principal,
        string? vin,
        int? year,
        string? make,
        string? model)
    {
        ApplicationUser? user =
            await _userManager.GetUserAsync(
                principal);

        if (user == null)
        {
            return;
        }

        string normalizedVin =
            NormalizeVin(vin);

        if (!IsValidVin(normalizedVin))
        {
            return;
        }

        UserVinHistory? existing =
            await _dbContext.UserVinHistories
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == user.Id &&
                        x.Vin == normalizedVin);

        if (existing == null)
        {
            existing =
                new UserVinHistory
                {
                    UserId = user.Id,
                    Vin = normalizedVin
                };

            _dbContext.UserVinHistories.Add(
                existing);
        }

        existing.Year =
            year ?? existing.Year;

        if (!string.IsNullOrWhiteSpace(make))
        {
            existing.Make =
                make.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            existing.Model =
                model.Trim();
        }

        existing.LastUsedOn =
            DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<UserVinHistory>> GetAsync(
        ClaimsPrincipal principal,
        int take = 25)
    {
        ApplicationUser? user =
            await _userManager.GetUserAsync(
                principal);

        if (user == null)
        {
            return new List<UserVinHistory>();
        }

        return await _dbContext.UserVinHistories
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.LastUsedOn)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync();
    }

    private static string NormalizeVin(
        string? vin)
    {
        return (vin ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty)
            .ToUpperInvariant();
    }

    private static bool IsValidVin(
        string vin)
    {
        return vin.Length == 17 &&
            vin.All(c =>
                char.IsLetterOrDigit(c)) &&
            !vin.Contains('I') &&
            !vin.Contains('O') &&
            !vin.Contains('Q');
    }
}
