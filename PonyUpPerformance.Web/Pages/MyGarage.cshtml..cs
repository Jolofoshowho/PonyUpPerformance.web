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
        private readonly VehicleRenderService _vehicleRenderService;
        private readonly NhtsaVehicleService _nhtsaVehicleService;

        public MyGarageModel(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    VehiclePaintPaletteService paintPaletteService,
    NhtsaVehicleService nhtsaVehicleService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _paintPaletteService = paintPaletteService;
            _nhtsaVehicleService = nhtsaVehicleService;
        }

        [BindProperty]
        public GarageVehicle NewVehicle { get; set; } = new GarageVehicle();

        public List<GarageVehicle> Vehicles { get; set; } = new List<GarageVehicle>();
        public GarageVehicle? SelectedVehicle { get; set; }
        public List<VehiclePaintColor> PaintColors { get; set; } = new List<VehiclePaintColor>();
        public List<AnalysisHistory> RecentAnalyses { get; set; } = new List<AnalysisHistory>();
        public string SelectedVehicleSvg { get; set; } = "";
        public int? PreviousVehicleId { get; set; }
        public int? NextVehicleId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? vehicleId)
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            Vehicles = await _dbContext.GarageVehicles
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            SelectedVehicle = vehicleId.HasValue
                ? Vehicles.FirstOrDefault(x => x.Id == vehicleId.Value)
                : Vehicles.FirstOrDefault();

            SetPreviousAndNextVehicleIds();

            if (SelectedVehicle != null)
            {
                PaintColors = _paintPaletteService.GetPaletteForMake(SelectedVehicle.Make);

                RecentAnalyses = await _dbContext.AnalysisHistories
                    .Where(x =>
                        x.UserId == user.Id &&
                        x.VehicleYear == SelectedVehicle.Year &&
                        x.VehicleMake == SelectedVehicle.Make &&
                        x.VehicleModel == SelectedVehicle.Model)
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(5)
                    .ToListAsync();
            }

            return Page();
        }
        public async Task<IActionResult> OnPostDecodeVinAsync()
        {
            var decoded = await _nhtsaVehicleService.DecodeVinAsync(NewVehicle.Vin);

            if (decoded.Success)
            {
                NewVehicle.Vin = decoded.Vin;
                NewVehicle.Year = decoded.Year;
                NewVehicle.Make = decoded.Make;
                NewVehicle.Model = decoded.Model;
                NewVehicle.Trim = decoded.Trim;
                NewVehicle.BodyStyle = decoded.BodyStyle;
                NewVehicle.Drivetrain = decoded.Drivetrain;
                NewVehicle.FuelType = decoded.FuelType;
                NewVehicle.Engine = decoded.Engine;
                NewVehicle.SelectedPaintHex = "#b8b8b8";
            }

            return await OnGetAsync(null);
        }
        public async Task<IActionResult> OnPostAddVehicleAsync()
        {
            ApplicationUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            NewVehicle.UserId = user.Id;
            NewVehicle.CreatedOn = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(NewVehicle.SelectedPaintHex))
            {
                NewVehicle.SelectedPaintHex = "#b8b8b8";
            }

            if (string.IsNullOrWhiteSpace(NewVehicle.BodyStyle))
            {
                NewVehicle.BodyStyle = "Sedan";
            }

            if (string.IsNullOrWhiteSpace(NewVehicle.FuelType))
            {
                NewVehicle.FuelType = "Gasoline";
            }

            if (string.IsNullOrWhiteSpace(NewVehicle.Engine))
                NewVehicle.Engine = "Unknown";

            if (string.IsNullOrWhiteSpace(NewVehicle.Trim))
                NewVehicle.Trim = "Base";

            if (string.IsNullOrWhiteSpace(NewVehicle.SelectedPaintName))
                NewVehicle.SelectedPaintName = "Unselected Color";

            if (string.IsNullOrWhiteSpace(NewVehicle.SelectedPaintCode))
                NewVehicle.SelectedPaintCode = "";

            _dbContext.GarageVehicles.Add(NewVehicle);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/MyGarage", new { vehicleId = NewVehicle.Id });
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
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            GarageVehicle? vehicle = await _dbContext.GarageVehicles
                .FirstOrDefaultAsync(x => x.Id == vehicleId && x.UserId == user.Id);

            if (vehicle == null)
            {
                return RedirectToPage("/MyGarage");
            }

            vehicle.SelectedPaintName = paintName ?? "";
            vehicle.SelectedPaintCode = paintCode ?? "";
            vehicle.SelectedPaintHex = paintHex ?? "#b8b8b8";

            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/MyGarage", new { vehicleId = vehicle.Id });
        }

        private void SetPreviousAndNextVehicleIds()
        {
            if (SelectedVehicle == null || Vehicles.Count == 0)
            {
                PreviousVehicleId = null;
                NextVehicleId = null;
                return;
            }

            int index = Vehicles.FindIndex(x => x.Id == SelectedVehicle.Id);

            if (index < 0)
            {
                PreviousVehicleId = null;
                NextVehicleId = null;
                return;
            }

            int previousIndex = index <= 0 ? Vehicles.Count - 1 : index - 1;
            int nextIndex = index >= Vehicles.Count - 1 ? 0 : index + 1;

            PreviousVehicleId = Vehicles[previousIndex].Id;
            NextVehicleId = Vehicles[nextIndex].Id;
        }
    }
}