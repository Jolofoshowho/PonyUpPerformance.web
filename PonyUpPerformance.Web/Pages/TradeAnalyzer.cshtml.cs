using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;
using PonyUpPerformance.Web.Services.Scoring;

namespace PonyUpPerformance.Web.Pages;

public class TradeAnalyzerModel : PageModel
{
    private readonly ITradeScoringService _tradeScoringService;
    private readonly IVinDecoderService _vinDecoderService;
    private readonly IVehicleSpecEnrichmentService _vehicleSpecEnrichmentService;
    private readonly IMarketValueService _marketValueService;

    public TradeAnalyzerModel(
        ITradeScoringService tradeScoringService,
        IVinDecoderService vinDecoderService,
        IVehicleSpecEnrichmentService vehicleSpecEnrichmentService,
        IMarketValueService marketValueService)
    {
        _tradeScoringService =
            tradeScoringService;

        _vinDecoderService =
            vinDecoderService;

        _vehicleSpecEnrichmentService =
            vehicleSpecEnrichmentService;

        _marketValueService =
            marketValueService;
    }

    [BindProperty]
    public TradeDecisionInput Input { get; set; } = new();

    public TradeDecisionResult? Result { get; private set; }

    public string YourVinMessage { get; private set; }
        = string.Empty;

    public string TheirVinMessage { get; private set; }
        = string.Empty;

    public string YourValueMessage { get; private set; }
        = string.Empty;

    public string TheirValueMessage { get; private set; }
        = string.Empty;

    public void OnGet()
    {
    }

    public Task<IActionResult> OnPostDecodeYourVinAsync(
        CancellationToken cancellationToken)
    {
        return DecodeVehicleAsync(
            isYourVehicle: true,
            cancellationToken);
    }

    public Task<IActionResult> OnPostDecodeTheirVinAsync(
        CancellationToken cancellationToken)
    {
        return DecodeVehicleAsync(
            isYourVehicle: false,
            cancellationToken);
    }

    public IActionResult OnPostAnalyze()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result =
            _tradeScoringService.Analyze(
                Input);

        return Page();
    }

    private async Task<IActionResult> DecodeVehicleAsync(
        bool isYourVehicle,
        CancellationToken cancellationToken)
    {
        string vin =
            isYourVehicle
                ? Input.YourVin ?? string.Empty
                : Input.TheirVin ?? string.Empty;

        string modelKey =
            isYourVehicle
                ? "Input.YourVin"
                : "Input.TheirVin";

        if (string.IsNullOrWhiteSpace(vin))
        {
            ModelState.Clear();

            ModelState.AddModelError(
                modelKey,
                "Enter a VIN to decode.");

            return Page();
        }

        VehicleProfile decoded =
            await _vinDecoderService.DecodeAsync(
                vin,
                cancellationToken);

        if (!decoded.DecodeSuccessful)
        {
            ModelState.Clear();

            string warning =
                decoded.DecodeWarnings
                    .FirstOrDefault()
                ?? "The VIN could not be decoded.";

            ModelState.AddModelError(
                modelKey,
                warning);

            return Page();
        }

        ApplyManualHints(
            decoded,
            isYourVehicle);

        decoded =
            await _vehicleSpecEnrichmentService.EnrichAsync(
                decoded,
                cancellationToken);

        int? mileage =
            isYourVehicle
                ? Input.YourMileage
                : Input.TheirMileage;

        if (mileage.HasValue)
        {
            decoded.CurrentMileage =
                mileage.Value;
        }

        ApplyDecodedVehicle(
            decoded,
            isYourVehicle);

        MarketValueResult valuation =
            await _marketValueService.AnalyzeAsync(
                decoded,
                cancellationToken);

        if (valuation.HasEstimate)
        {
            if (isYourVehicle)
            {
                if (!Input.YourValue.HasValue)
                {
                    Input.YourValue =
                        valuation.EstimatedMarketValue;
                }

                YourValueMessage =
                    $"PonyUp market value: {valuation.EstimatedMarketValue:C0}";
            }
            else
            {
                if (!Input.TheirValue.HasValue)
                {
                    Input.TheirValue =
                        valuation.EstimatedMarketValue;
                }

                TheirValueMessage =
                    $"PonyUp market value: {valuation.EstimatedMarketValue:C0}";
            }
        }
        else
        {
            if (isYourVehicle)
            {
                YourValueMessage =
                    valuation.Summary;
            }
            else
            {
                TheirValueMessage =
                    valuation.Summary;
            }
        }

        string displayName =
            decoded.DisplayName;

        if (isYourVehicle)
        {
            YourVinMessage =
                string.IsNullOrWhiteSpace(displayName)
                    ? "VIN decoded."
                    : displayName;
        }
        else
        {
            TheirVinMessage =
                string.IsNullOrWhiteSpace(displayName)
                    ? "VIN decoded."
                    : displayName;
        }

        ModelState.Clear();

        return Page();
    }

    private void ApplyManualHints(
        VehicleProfile vehicle,
        bool isYourVehicle)
    {
        string? trim =
            isYourVehicle
                ? Input.YourTrim
                : Input.TheirTrim;

        string? engine =
            isYourVehicle
                ? Input.YourEngine
                : Input.TheirEngine;

        string? transmission =
            isYourVehicle
                ? Input.YourTransmission
                : Input.TheirTransmission;

        string? drivetrain =
            isYourVehicle
                ? Input.YourDrivetrain
                : Input.TheirDrivetrain;

        string? fuel =
            isYourVehicle
                ? Input.YourFuelType
                : Input.TheirFuelType;

        if (string.IsNullOrWhiteSpace(
                vehicle.Trim) &&
            !string.IsNullOrWhiteSpace(
                trim))
        {
            vehicle.Trim =
                trim;
        }

        if (string.IsNullOrWhiteSpace(
                vehicle.Engine) &&
            !string.IsNullOrWhiteSpace(
                engine))
        {
            vehicle.Engine =
                engine;
        }

        if (string.IsNullOrWhiteSpace(
                vehicle.Transmission) &&
            !string.IsNullOrWhiteSpace(
                transmission))
        {
            vehicle.Transmission =
                transmission;
        }

        if (string.IsNullOrWhiteSpace(
                vehicle.Drivetrain) &&
            !string.IsNullOrWhiteSpace(
                drivetrain))
        {
            vehicle.Drivetrain =
                drivetrain;
        }

        if (string.IsNullOrWhiteSpace(
                vehicle.FuelType) &&
            !string.IsNullOrWhiteSpace(
                fuel))
        {
            vehicle.FuelType =
                fuel;
        }
    }

    private void ApplyDecodedVehicle(
        VehicleProfile vehicle,
        bool isYourVehicle)
    {
        if (isYourVehicle)
        {
            if (!string.IsNullOrWhiteSpace(
                vehicle.Vin))
            {
                Input.YourVin =
                    vehicle.Vin;
            }

            if (vehicle.Year.HasValue)
            {
                Input.YourYear =
                    vehicle.Year;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Make))
            {
                Input.YourMake =
                    vehicle.Make;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Model))
            {
                Input.YourModel =
                    vehicle.Model;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Trim))
            {
                Input.YourTrim =
                    vehicle.Trim;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Engine))
            {
                Input.YourEngine =
                    vehicle.Engine;
            }

            if (vehicle.Horsepower.HasValue)
            {
                Input.YourHorsepower =
                    vehicle.Horsepower;
            }

            if (vehicle.TorqueLbFt.HasValue)
            {
                Input.YourTorqueLbFt =
                    vehicle.TorqueLbFt;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Transmission))
            {
                Input.YourTransmission =
                    vehicle.Transmission;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.Drivetrain))
            {
                Input.YourDrivetrain =
                    vehicle.Drivetrain;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.FuelType))
            {
                Input.YourFuelType =
                    vehicle.FuelType;
            }

            if (!string.IsNullOrWhiteSpace(
                vehicle.BodyStyle))
            {
                Input.YourBodyStyle =
                    vehicle.BodyStyle;
            }

            if (vehicle.CityMpg.HasValue)
            {
                Input.YourCityMpg =
                    vehicle.CityMpg;
            }

            if (vehicle.HighwayMpg.HasValue)
            {
                Input.YourHighwayMpg =
                    vehicle.HighwayMpg;
            }

            if (vehicle.CurbWeightLbs.HasValue)
            {
                Input.YourCurbWeightLbs =
                    vehicle.CurbWeightLbs;
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Vin))
        {
            Input.TheirVin =
                vehicle.Vin;
        }

        if (vehicle.Year.HasValue)
        {
            Input.TheirYear =
                vehicle.Year;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Make))
        {
            Input.TheirMake =
                vehicle.Make;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Model))
        {
            Input.TheirModel =
                vehicle.Model;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Trim))
        {
            Input.TheirTrim =
                vehicle.Trim;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Engine))
        {
            Input.TheirEngine =
                vehicle.Engine;
        }

        if (vehicle.Horsepower.HasValue)
        {
            Input.TheirHorsepower =
                vehicle.Horsepower;
        }

        if (vehicle.TorqueLbFt.HasValue)
        {
            Input.TheirTorqueLbFt =
                vehicle.TorqueLbFt;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Transmission))
        {
            Input.TheirTransmission =
                vehicle.Transmission;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.Drivetrain))
        {
            Input.TheirDrivetrain =
                vehicle.Drivetrain;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.FuelType))
        {
            Input.TheirFuelType =
                vehicle.FuelType;
        }

        if (!string.IsNullOrWhiteSpace(
            vehicle.BodyStyle))
        {
            Input.TheirBodyStyle =
                vehicle.BodyStyle;
        }

        if (vehicle.CityMpg.HasValue)
        {
            Input.TheirCityMpg =
                vehicle.CityMpg;
        }

        if (vehicle.HighwayMpg.HasValue)
        {
            Input.TheirHighwayMpg =
                vehicle.HighwayMpg;
        }

        if (vehicle.CurbWeightLbs.HasValue)
        {
            Input.TheirCurbWeightLbs =
                vehicle.CurbWeightLbs;
        }
    }
}
