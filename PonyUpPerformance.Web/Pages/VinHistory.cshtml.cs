using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages;

[Authorize]
public sealed class VinHistoryModel : PageModel
{
    private readonly UserVinHistoryService _vinHistoryService;

    public VinHistoryModel(
        UserVinHistoryService vinHistoryService)
    {
        _vinHistoryService =
            vinHistoryService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var history =
            await _vinHistoryService.GetAsync(
                User,
                25);

        return new JsonResult(
            history.Select(
                x => new
                {
                    vin =
                        x.Vin,

                    year =
                        x.Year,

                    make =
                        x.Make,

                    model =
                        x.Model,

                    lastUsedOn =
                        x.LastUsedOn
                }));
    }

    public async Task<IActionResult> OnPostSaveAsync(
        string? vin,
        int? year,
        string? make,
        string? model)
    {
        await _vinHistoryService.SaveAsync(
            User,
            vin,
            year,
            make,
            model);

        return new JsonResult(
            new
            {
                ok = true
            });
    }
}
