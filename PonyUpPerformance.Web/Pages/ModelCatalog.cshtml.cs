using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages;

public class ModelCatalogModel : PageModel
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UsageCreditService _usageCreditService;
    private readonly IWebHostEnvironment _environment;

    public ModelCatalogModel(
        ApplicationDbContext dbContext,
        UsageCreditService usageCreditService,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _usageCreditService = usageCreditService;
        _environment = environment;
    }

    public List<CatalogEntry> Catalog { get; private set; } = new();
    public List<DemandEntry> Demand { get; private set; } = new();

    public int LiveModelCount =>
        Catalog.Count(x => x.Enabled && x.AssetExists);

    public int PlannedModelCount =>
        Catalog.Count;

    public int MissingDemandCount =>
        Demand.Count(x => !x.HasPonyUpModel);

    public async Task<IActionResult> OnGetAsync()
    {
        UsageCreditStatus access =
            await _usageCreditService.GetStatusAsync(User);

        if (!string.Equals(
                access.CurrentPlan,
                "Owner",
                StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        Catalog =
            await LoadCatalogAsync();

        Demand =
            await LoadDemandAsync(
                Catalog);

        return Page();
    }

    private async Task<List<CatalogEntry>>
        LoadCatalogAsync()
    {
        string path =
            Path.Combine(
                _environment.WebRootPath,
                "models",
                "vehicle-manifest.json");

        if (!System.IO.File.Exists(path))
        {
            return new();
        }

        await using FileStream stream =
            System.IO.File.OpenRead(path);

        using JsonDocument document =
            await JsonDocument.ParseAsync(stream);

        if (!document.RootElement.TryGetProperty(
                "vehicles",
                out JsonElement vehicles) ||
            vehicles.ValueKind != JsonValueKind.Array)
        {
            return new();
        }

        List<CatalogEntry> entries = new();

        foreach (JsonElement element in
                 vehicles.EnumerateArray())
        {
            string asset =
                GetString(
                    element,
                    "asset");

            bool enabled =
                !element.TryGetProperty(
                    "enabled",
                    out JsonElement enabledElement) ||
                enabledElement.ValueKind != JsonValueKind.False;

            JsonElement match =
                element.TryGetProperty(
                    "match",
                    out JsonElement matchElement)
                    ? matchElement
                    : default;

            int? exactYear =
                GetInt(
                    match,
                    "year");

            int? yearMin =
                GetInt(
                    match,
                    "yearMin");

            int? yearMax =
                GetInt(
                    match,
                    "yearMax");

            bool remoteAsset =
                Uri.TryCreate(
                    asset,
                    UriKind.Absolute,
                    out Uri? assetUri) &&
                (assetUri.Scheme == Uri.UriSchemeHttp ||
                 assetUri.Scheme == Uri.UriSchemeHttps);

            string physicalAsset =
                string.IsNullOrWhiteSpace(asset) ||
                remoteAsset
                    ? string.Empty
                    : Path.Combine(
                        _environment.WebRootPath,
                        asset
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar));

            entries.Add(
                new CatalogEntry
                {
                    Id =
                        GetString(
                            element,
                            "id"),

                    Make =
                        GetString(
                            match,
                            "make"),

                    Model =
                        GetString(
                            match,
                            "model"),

                    Trim =
                        GetString(
                            match,
                            "trim"),

                    Year =
                        exactYear,

                    YearMin =
                        yearMin,

                    YearMax =
                        yearMax,

                    Asset =
                        asset,

                    Enabled =
                        enabled,

                    AssetExists =
                        remoteAsset ||
                        (!string.IsNullOrWhiteSpace(
                            physicalAsset) &&
                         System.IO.File.Exists(
                            physicalAsset)),

                    Notes =
                        GetString(
                            element,
                            "notes"),

                    InteriorReady =
                        GetBool(
                            element,
                            "quality",
                            "interiorReady"),

                    WheelsReady =
                        GetBool(
                            element,
                            "quality",
                            "wheelsReady"),

                    PaintReady =
                        GetBool(
                            element,
                            "quality",
                            "paintReady")
                });
        }

        return entries
            .OrderBy(x => x.Make)
            .ThenBy(x => x.Model)
            .ThenBy(x => x.YearMin ?? x.Year ?? 0)
            .ToList();
    }

    private async Task<List<DemandEntry>>
        LoadDemandAsync(
            List<CatalogEntry> catalog)
    {
        var garageRows =
            await _dbContext.GarageVehicles
                .AsNoTracking()
                .Where(x =>
                    x.Year > 0 &&
                    x.Make != "" &&
                    x.Model != "")
                .Select(x => new
                {
                    x.Year,
                    x.Make,
                    x.Model
                })
                .ToListAsync();

        var analysisRows =
            await _dbContext.AnalysisHistories
                .AsNoTracking()
                .Where(x =>
                    x.VehicleYear > 0 &&
                    x.VehicleMake != "" &&
                    x.VehicleModel != "")
                .Select(x => new
                {
                    Year =
                        x.VehicleYear,

                    Make =
                        x.VehicleMake,

                    Model =
                        x.VehicleModel
                })
                .ToListAsync();

        return garageRows
            .Concat(analysisRows)
            .Select(x => new
            {
                x.Year,

                Make =
                    x.Make.Trim(),

                Model =
                    x.Model.Trim(),

                NormalizedMake =
                    x.Make
                        .Trim()
                        .ToUpperInvariant(),

                NormalizedModel =
                    x.Model
                        .Trim()
                        .ToUpperInvariant()
            })
            .GroupBy(x => new
            {
                x.Year,
                x.NormalizedMake,
                x.NormalizedModel
            })
            .Select(group =>
            {
                var sample =
                    group.First();

                bool hasModel =
                    catalog.Any(entry =>
                        entry.Enabled &&
                        entry.AssetExists &&
                        Matches(
                            entry,
                            group.Key.Year,
                            sample.Make,
                            sample.Model));

                return new DemandEntry
                {
                    Year =
                        group.Key.Year,

                    Make =
                        sample.Make,

                    Model =
                        sample.Model,

                    Uses =
                        group.Count(),

                    HasPonyUpModel =
                        hasModel
                };
            })
            .OrderBy(x =>
                x.HasPonyUpModel)
            .ThenByDescending(x =>
                x.Uses)
            .ThenByDescending(x =>
                x.Year)
            .ThenBy(x =>
                x.Make)
            .ThenBy(x =>
                x.Model)
            .Take(250)
            .ToList();
    }

    private static bool Matches(
        CatalogEntry entry,
        int year,
        string make,
        string model)
    {
        if (!string.Equals(
                entry.Make,
                make,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                entry.Model,
                model,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (entry.Year.HasValue)
        {
            return entry.Year.Value ==
                year;
        }

        if (entry.YearMin.HasValue &&
            year < entry.YearMin.Value)
        {
            return false;
        }

        if (entry.YearMax.HasValue &&
            year > entry.YearMax.Value)
        {
            return false;
        }

        return true;
    }

    private static string GetString(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind !=
                JsonValueKind.Object ||
            !element.TryGetProperty(
                propertyName,
                out JsonElement value) ||
            value.ValueKind !=
                JsonValueKind.String)
        {
            return string.Empty;
        }

        return value.GetString()
            ?? string.Empty;
    }

    private static int? GetInt(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind !=
                JsonValueKind.Object ||
            !element.TryGetProperty(
                propertyName,
                out JsonElement value) ||
            !value.TryGetInt32(
                out int result))
        {
            return null;
        }

        return result;
    }

    private static bool GetBool(
        JsonElement element,
        string objectPropertyName,
        string boolPropertyName)
    {
        if (element.ValueKind !=
                JsonValueKind.Object ||
            !element.TryGetProperty(
                objectPropertyName,
                out JsonElement nested) ||
            nested.ValueKind !=
                JsonValueKind.Object ||
            !nested.TryGetProperty(
                boolPropertyName,
                out JsonElement value))
        {
            return false;
        }

        return value.ValueKind ==
            JsonValueKind.True;
    }

    public sealed class CatalogEntry
    {
        public string Id { get; set; } = "";
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public string Trim { get; set; } = "";
        public int? Year { get; set; }
        public int? YearMin { get; set; }
        public int? YearMax { get; set; }
        public string Asset { get; set; } = "";
        public bool Enabled { get; set; }
        public bool AssetExists { get; set; }
        public string Notes { get; set; } = "";
        public bool PaintReady { get; set; }
        public bool InteriorReady { get; set; }
        public bool WheelsReady { get; set; }

        public bool CustomizationReady =>
            PaintReady &&
            InteriorReady &&
            WheelsReady;

        public string YearLabel =>
            Year.HasValue
                ? Year.Value.ToString()
                : YearMin.HasValue ||
                  YearMax.HasValue
                    ? $"{YearMin?.ToString() ?? "?"}–{YearMax?.ToString() ?? "?"}"
                    : "Any";
    }

    public sealed class DemandEntry
    {
        public int Year { get; set; }
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public int Uses { get; set; }
        public bool HasPonyUpModel { get; set; }
    }

}
