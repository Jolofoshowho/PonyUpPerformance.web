using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PonyUpPerformance.Web.Data;
using PonyUpPerformance.Web.Models;
using PonyUpPerformance.Web.Services;

namespace PonyUpPerformance.Web.Pages
{
    public class MyGarageModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VehiclePaintPaletteService _paintPaletteService;
        private readonly IVinDecoderService _vinDecoderService;
        private readonly VehicleRenderService _vehicleRenderService;
        private readonly UsageCreditService _usageCreditService;

        public MyGarageModel(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            VehiclePaintPaletteService paintPaletteService,
            IVinDecoderService vinDecoderService,
            VehicleRenderService vehicleRenderService,
            UsageCreditService usageCreditService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _paintPaletteService = paintPaletteService;
            _vinDecoderService = vinDecoderService;
            _vehicleRenderService = vehicleRenderService;
            _usageCreditService = usageCreditService;
        }

        [BindProperty]
        public GarageVehicle NewVehicle { get; set; } = new();

        public List<GarageVehicle> Vehicles { get; set; } = new();
        public GarageVehicle? SelectedVehicle { get; set; }

        public List<VehiclePaintColor> PaintColors { get; set; } = new();
        public List<AnalysisHistory> RecentAnalyses { get; set; } = new();
        public List<AnalysisHistory> AllAnalyses { get; set; } = new();

        public string SelectedVehicleSvg { get; set; } = "";

        public Dictionary<int, string> VehicleThumbnailSvgs { get; set; } = new();

        public int? PreviousVehicleId { get; set; }
        public int? NextVehicleId { get; set; }

        public string UserEmail { get; set; } = "";
        public string CurrentPlan { get; set; } = "Free";
        public int RemainingCredits { get; set; }
        public string AnalysisAccessLabel { get; set; } = "0";
        public bool IsOwnerAccess { get; set; }

        public async Task<IActionResult> OnGetAsync(int? vehicleId)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            await LoadGarageAsync(user, vehicleId);

            return Page();
        }

        public async Task<IActionResult> OnPostDecodeVinAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            NewVehicle.Vin = (NewVehicle.Vin ?? "").Trim();

            if (string.IsNullOrWhiteSpace(NewVehicle.Vin))
            {
                ModelState.AddModelError(
                    "NewVehicle.Vin",
                    "Enter a VIN to decode, or fill the vehicle in manually.");

                await LoadGarageAsync(user, null);

                return Page();
            }

            VehicleProfile decoded =
                await _vinDecoderService.DecodeAsync(NewVehicle.Vin);

            if (decoded.DecodeSuccessful)
            {
                NewVehicle.Vin = decoded.Vin;

                if (decoded.Year.HasValue)
                {
                    NewVehicle.Year = decoded.Year.Value;
                }

                NewVehicle.Make = decoded.Make ?? "";
                NewVehicle.Model = decoded.Model ?? "";
                NewVehicle.Trim = decoded.Trim ?? "";
                NewVehicle.BodyStyle = decoded.BodyStyle ?? "";
                NewVehicle.Drivetrain = decoded.Drivetrain ?? "";
                NewVehicle.FuelType = decoded.FuelType ?? "";
                NewVehicle.Engine = decoded.Engine ?? "";

                VehicleRenderProfile render =
                    _vehicleRenderService.GetRenderProfile(NewVehicle);

                NewVehicle.RenderType = render.RenderType;

                if (string.IsNullOrWhiteSpace(NewVehicle.SelectedPaintHex))
                {
                    NewVehicle.SelectedPaintHex = "#b8b8b8";
                }

                ModelState.Clear();
            }
            else
            {
                string warning =
                    decoded.DecodeWarnings.FirstOrDefault()
                    ?? "The VIN could not be decoded.";

                ModelState.AddModelError(
                    "NewVehicle.Vin",
                    warning);
            }

            await LoadGarageAsync(user, null);

            return Page();
        }

        public async Task<IActionResult> OnPostAddVehicleAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            NormalizeNewVehicle();

            NewVehicle.UserId = user.Id;
            NewVehicle.CreatedOn = DateTime.UtcNow;

            VehicleRenderProfile render =
                _vehicleRenderService.GetRenderProfile(NewVehicle);

            NewVehicle.RenderType = render.RenderType;

            _dbContext.GarageVehicles.Add(NewVehicle);

            await _dbContext.SaveChangesAsync();

            TempData["GarageMessage"] =
                "Vehicle added to your PonyUp Garage.";

            return RedirectToPage(
                "/MyGarage",
                new { vehicleId = NewVehicle.Id });
        }

        public async Task<IActionResult> OnPostSelectPaintAsync(
            int vehicleId,
            string paintName,
            string paintCode,
            string paintHex)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            GarageVehicle? vehicle =
                await _dbContext.GarageVehicles
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == vehicleId &&
                            x.UserId == user.Id);

            if (vehicle == null)
            {
                return RedirectToPage("/MyGarage");
            }

            vehicle.SelectedPaintName =
                (paintName ?? "").Trim();

            vehicle.SelectedPaintCode =
                (paintCode ?? "").Trim();

            vehicle.SelectedPaintHex =
                string.IsNullOrWhiteSpace(paintHex)
                    ? "#b8b8b8"
                    : paintHex.Trim();

            await _dbContext.SaveChangesAsync();

            TempData["GarageMessage"] =
                $"Paint updated to {vehicle.SelectedPaintName}.";

            return RedirectToPage(
                "/MyGarage",
                new { vehicleId = vehicle.Id });
        }

        public async Task<IActionResult> OnPostUpdateMileageAsync(
            int vehicleId,
            int mileage)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            GarageVehicle? vehicle =
                await _dbContext.GarageVehicles
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == vehicleId &&
                            x.UserId == user.Id);

            if (vehicle == null)
            {
                return RedirectToPage("/MyGarage");
            }

            vehicle.Mileage = Math.Max(0, mileage);

            await _dbContext.SaveChangesAsync();

            TempData["GarageMessage"] =
                "Mileage updated.";

            return RedirectToPage(
                "/MyGarage",
                new { vehicleId = vehicle.Id });
        }

        public async Task<IActionResult> OnPostDeleteVehicleAsync(
            int vehicleId)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new { area = "Identity" });
            }

            GarageVehicle? vehicle =
                await _dbContext.GarageVehicles
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == vehicleId &&
                            x.UserId == user.Id);

            if (vehicle != null)
            {
                _dbContext.GarageVehicles.Remove(vehicle);

                await _dbContext.SaveChangesAsync();

                TempData["GarageMessage"] =
                    "Vehicle removed from your PonyUp Garage.";
            }

            return RedirectToPage("/MyGarage");
        }

        private async Task LoadGarageAsync(
            ApplicationUser user,
            int? vehicleId)
        {
            UserEmail = user.Email ?? user.UserName ?? "";

            UsageCreditStatus access =
                await _usageCreditService.GetStatusAsync(User);

            CurrentPlan = string.IsNullOrWhiteSpace(access.CurrentPlan)
                ? "Free"
                : access.CurrentPlan;

            RemainingCredits = access.RemainingCredits;
            IsOwnerAccess = string.Equals(
                CurrentPlan,
                "Owner",
                StringComparison.OrdinalIgnoreCase);

            AnalysisAccessLabel =
                IsOwnerAccess
                    ? "👑 ☁️ ∞"
                    : string.Equals(
                        CurrentPlan,
                        "Unlimited",
                        StringComparison.OrdinalIgnoreCase)
                        ? "∞"
                        : RemainingCredits.ToString();

            AllAnalyses =
                await _dbContext.AnalysisHistories
                    .Where(x => x.UserId == user.Id)
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(50)
                    .ToListAsync();

            Vehicles = await _dbContext.GarageVehicles
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            VehicleThumbnailSvgs = Vehicles.ToDictionary(
                vehicle => vehicle.Id,
                vehicle => _vehicleRenderService.BuildVehicleSvg(vehicle));

            SelectedVehicle = vehicleId.HasValue
                ? Vehicles.FirstOrDefault(
                    x => x.Id == vehicleId.Value)
                : Vehicles.FirstOrDefault();

            if (vehicleId.HasValue &&
                SelectedVehicle == null)
            {
                SelectedVehicle = Vehicles.FirstOrDefault();
            }

            SetPreviousAndNextVehicleIds();

            if (SelectedVehicle == null)
            {
                PaintColors = new List<VehiclePaintColor>();
                RecentAnalyses = new List<AnalysisHistory>();
                SelectedVehicleSvg = "";
                return;
            }

            PaintColors =
                _paintPaletteService.GetPaletteForMake(
                    SelectedVehicle.Make);

            SelectedVehicleSvg =
                _vehicleRenderService.BuildVehicleSvg(
                    SelectedVehicle);

            RecentAnalyses =
                await _dbContext.AnalysisHistories
                    .Where(x =>
                        x.UserId == user.Id &&
                        x.VehicleYear == SelectedVehicle.Year &&
                        x.VehicleMake == SelectedVehicle.Make &&
                        x.VehicleModel == SelectedVehicle.Model)
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(5)
                    .ToListAsync();
        }

        private void NormalizeNewVehicle()
        {
            NewVehicle.Vin =
                (NewVehicle.Vin ?? "").Trim();

            NewVehicle.Make =
                (NewVehicle.Make ?? "").Trim();

            NewVehicle.Model =
                (NewVehicle.Model ?? "").Trim();

            NewVehicle.Trim =
                (NewVehicle.Trim ?? "").Trim();

            NewVehicle.BodyStyle =
                (NewVehicle.BodyStyle ?? "").Trim();

            NewVehicle.Drivetrain =
                (NewVehicle.Drivetrain ?? "").Trim();

            NewVehicle.FuelType =
                (NewVehicle.FuelType ?? "").Trim();

            NewVehicle.Engine =
                (NewVehicle.Engine ?? "").Trim();

            NewVehicle.SelectedPaintName =
                (NewVehicle.SelectedPaintName ?? "").Trim();

            NewVehicle.SelectedPaintCode =
                (NewVehicle.SelectedPaintCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(
                NewVehicle.SelectedPaintHex))
            {
                NewVehicle.SelectedPaintHex =
                    "#b8b8b8";
            }

            NewVehicle.Year =
                Math.Max(0, NewVehicle.Year);

            NewVehicle.Mileage =
                Math.Max(0, NewVehicle.Mileage);
        }

        private void SetPreviousAndNextVehicleIds()
        {
            if (SelectedVehicle == null ||
                Vehicles.Count == 0)
            {
                PreviousVehicleId = null;
                NextVehicleId = null;
                return;
            }

            int index =
                Vehicles.FindIndex(
                    x => x.Id == SelectedVehicle.Id);

            if (index < 0)
            {
                PreviousVehicleId = null;
                NextVehicleId = null;
                return;
            }

            if (Vehicles.Count == 1)
            {
                PreviousVehicleId = null;
                NextVehicleId = null;
                return;
            }

            int previousIndex =
                index <= 0
                    ? Vehicles.Count - 1
                    : index - 1;

            int nextIndex =
                index >= Vehicles.Count - 1
                    ? 0
                    : index + 1;

            PreviousVehicleId =
                Vehicles[previousIndex].Id;

            NextVehicleId =
                Vehicles[nextIndex].Id;
        }
    }
}
