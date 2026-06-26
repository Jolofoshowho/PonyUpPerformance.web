using PonyUpPerformance.Web.Models;
using System.Net;

namespace PonyUpPerformance.Web.Services
{
    public class VehicleRenderService
    {
        public VehicleRenderProfile GetRenderProfile(GarageVehicle vehicle)
        {
            string make = Normalize(vehicle.Make);
            string model = Normalize(vehicle.Model);
            string body = Normalize(vehicle.BodyStyle);

            if (body.Contains("truck") || model.Contains("f-150") || model.Contains("silverado") || model.Contains("sierra") || model.Contains("ram"))
            {
                return new VehicleRenderProfile
                {
                    BodyStyle = "Truck",
                    RenderType = "Truck",
                    Length = 292,
                    Height = 104,
                    HoodLength = 78,
                    CabinLength = 92,
                    DeckLength = 100,
                    RoofHeight = 48,
                    WheelBase = 205,
                    GroundClearance = 24,
                    HasTruckBed = true
                };
            }

            if (body.Contains("suv") || model.Contains("tahoe") || model.Contains("suburban") || model.Contains("explorer") || model.Contains("wrangler"))
            {
                return new VehicleRenderProfile
                {
                    BodyStyle = "SUV",
                    RenderType = "SUV",
                    Length = 280,
                    Height = 112,
                    HoodLength = 68,
                    CabinLength = 140,
                    DeckLength = 50,
                    RoofHeight = 58,
                    WheelBase = 190,
                    GroundClearance = 26,
                    IsSuv = true
                };
            }

            if (body.Contains("coupe") || model.Contains("corvette") || model.Contains("camaro") || model.Contains("mustang") || model.Contains("challenger"))
            {
                return new VehicleRenderProfile
                {
                    BodyStyle = "Coupe",
                    RenderType = "Coupe",
                    Length = 270,
                    Height = 82,
                    HoodLength = 92,
                    CabinLength = 82,
                    DeckLength = 70,
                    RoofHeight = 34,
                    WheelBase = 178,
                    GroundClearance = 14,
                    IsCoupe = true,
                    IsMuscleCar = model.Contains("camaro") || model.Contains("mustang") || model.Contains("challenger")
                };
            }

            if (make.Contains("pontiac") && model.Contains("grand prix"))
            {
                return new VehicleRenderProfile
                {
                    BodyStyle = "Sedan",
                    RenderType = "Long Nose Sedan",
                    Length = 282,
                    Height = 88,
                    HoodLength = 88,
                    CabinLength = 112,
                    DeckLength = 58,
                    RoofHeight = 38,
                    WheelBase = 190,
                    GroundClearance = 15
                };
            }

            return new VehicleRenderProfile();
        }

        public string BuildVehicleSvg(GarageVehicle vehicle)
        {
            VehicleRenderProfile profile = GetRenderProfile(vehicle);

            string paint = string.IsNullOrWhiteSpace(vehicle.SelectedPaintHex)
                ? "#b8b8b8"
                : vehicle.SelectedPaintHex;

            string make = WebUtility.HtmlEncode(vehicle.Make);
            string model = WebUtility.HtmlEncode(vehicle.Model);
            string label = WebUtility.HtmlEncode($"{vehicle.Year} {vehicle.Make} {vehicle.Model}");

            if (profile.HasTruckBed)
            {
                return BuildTruckSvg(profile, paint, label, make, model);
            }

            if (profile.IsSuv)
            {
                return BuildSuvSvg(profile, paint, label, make, model);
            }

            if (profile.IsCoupe)
            {
                return BuildCoupeSvg(profile, paint, label, make, model);
            }

            return BuildSedanSvg(profile, paint, label, make, model);
        }

        private static string BuildSedanSvg(VehicleRenderProfile p, string paint, string label, string make, string model)
        {
            return $@"
<svg class=""garage-render-svg"" viewBox=""0 0 420 210"" role=""img"" aria-label=""{label}"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <linearGradient id=""bodyGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#ffffff"" stop-opacity="".38""/>
      <stop offset=""35%"" stop-color=""{paint}""/>
      <stop offset=""100%"" stop-color=""#050505"" stop-opacity="".45""/>
    </linearGradient>
    <linearGradient id=""glassGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#d8f4ff""/>
      <stop offset=""100%"" stop-color=""#1a2a35""/>
    </linearGradient>
  </defs>

  <ellipse cx=""210"" cy=""174"" rx=""170"" ry=""18"" fill=""#000"" opacity="".55""/>

  <path d=""M58 126 C82 96, 112 88, 153 84 L240 82 C278 86, 319 103, 356 129 L369 151 C330 158, 105 158, 45 151 Z""
        fill=""url(#bodyGrad)"" stroke=""#f2f2f2"" stroke-width=""3""/>

  <path d=""M145 86 C168 52, 236 50, 270 88 L250 112 L132 112 Z""
        fill=""{paint}"" stroke=""#f2f2f2"" stroke-width=""3"" opacity="".96""/>

  <path d=""M154 88 C172 65, 198 60, 211 64 L211 108 L137 108 Z"" fill=""url(#glassGrad)"" opacity="".9""/>
  <path d=""M218 64 C242 65, 258 76, 266 108 L216 108 Z"" fill=""url(#glassGrad)"" opacity="".9""/>

  <line x1=""211"" y1=""64"" x2=""211"" y2=""112"" stroke=""#111"" stroke-width=""3"" opacity="".75""/>
  <line x1=""82"" y1=""132"" x2=""344"" y2=""132"" stroke=""#ffffff"" stroke-width=""2"" opacity="".35""/>

  <circle cx=""115"" cy=""153"" r=""30"" fill=""#050505"" stroke=""#111"" stroke-width=""6""/>
  <circle cx=""115"" cy=""153"" r=""15"" fill=""#bfc3c7"" stroke=""#333"" stroke-width=""4""/>
  <circle cx=""305"" cy=""153"" r=""30"" fill=""#050505"" stroke=""#111"" stroke-width=""6""/>
  <circle cx=""305"" cy=""153"" r=""15"" fill=""#bfc3c7"" stroke=""#333"" stroke-width=""4""/>

  <rect x=""64"" y=""134"" width=""32"" height=""8"" rx=""4"" fill=""#ff3131"" opacity="".8""/>
  <rect x=""326"" y=""134"" width=""28"" height=""8"" rx=""4"" fill=""#f6f1c6"" opacity="".85""/>

  <text x=""210"" y=""198"" text-anchor=""middle"" fill=""#ffffff"" font-size=""12"" font-family=""Arial"" opacity="".85"">{make} {model}</text>
</svg>";
        }

        private static string BuildCoupeSvg(VehicleRenderProfile p, string paint, string label, string make, string model)
        {
            return $@"
<svg class=""garage-render-svg"" viewBox=""0 0 420 210"" role=""img"" aria-label=""{label}"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <linearGradient id=""bodyGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#ffffff"" stop-opacity="".35""/>
      <stop offset=""35%"" stop-color=""{paint}""/>
      <stop offset=""100%"" stop-color=""#050505"" stop-opacity="".48""/>
    </linearGradient>
    <linearGradient id=""glassGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#d8f4ff""/>
      <stop offset=""100%"" stop-color=""#172632""/>
    </linearGradient>
  </defs>

  <ellipse cx=""210"" cy=""176"" rx=""172"" ry=""17"" fill=""#000"" opacity="".58""/>

  <path d=""M40 138 C76 106, 114 97, 165 93 L241 89 C286 94, 330 111, 378 141 L365 154 C295 160, 106 160, 47 152 Z""
        fill=""url(#bodyGrad)"" stroke=""#f2f2f2"" stroke-width=""3""/>

  <path d=""M154 94 C177 62, 227 61, 255 94 L238 113 L135 113 Z""
        fill=""{paint}"" stroke=""#f2f2f2"" stroke-width=""3""/>

  <path d=""M160 94 C178 73, 205 68, 221 72 L235 110 L142 110 Z"" fill=""url(#glassGrad)"" opacity="".9""/>

  <path d=""M63 137 C92 126, 133 121, 177 120"" stroke=""#ffffff"" stroke-width=""2"" opacity="".35""/>
  <path d=""M256 120 C296 123, 335 130, 365 143"" stroke=""#ffffff"" stroke-width=""2"" opacity="".35""/>

  <circle cx=""116"" cy=""154"" r=""31"" fill=""#050505"" stroke=""#111"" stroke-width=""6""/>
  <circle cx=""116"" cy=""154"" r=""16"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>
  <circle cx=""306"" cy=""154"" r=""31"" fill=""#050505"" stroke=""#111"" stroke-width=""6""/>
  <circle cx=""306"" cy=""154"" r=""16"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>

  <text x=""210"" y=""198"" text-anchor=""middle"" fill=""#ffffff"" font-size=""12"" font-family=""Arial"" opacity="".85"">{make} {model}</text>
</svg>";
        }

        private static string BuildTruckSvg(VehicleRenderProfile p, string paint, string label, string make, string model)
        {
            return $@"
<svg class=""garage-render-svg"" viewBox=""0 0 420 210"" role=""img"" aria-label=""{label}"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <linearGradient id=""bodyGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#ffffff"" stop-opacity="".32""/>
      <stop offset=""35%"" stop-color=""{paint}""/>
      <stop offset=""100%"" stop-color=""#050505"" stop-opacity="".5""/>
    </linearGradient>
    <linearGradient id=""glassGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#d8f4ff""/>
      <stop offset=""100%"" stop-color=""#1a2a35""/>
    </linearGradient>
  </defs>

  <ellipse cx=""210"" cy=""178"" rx=""176"" ry=""18"" fill=""#000"" opacity="".58""/>

  <path d=""M50 119 L128 119 C143 83, 207 82, 232 119 L368 119 L382 153 L43 153 Z""
        fill=""url(#bodyGrad)"" stroke=""#f2f2f2"" stroke-width=""3""/>

  <path d=""M135 118 L151 91 L204 91 L225 118 Z"" fill=""url(#glassGrad)"" stroke=""#111"" stroke-width=""2"" opacity="".92""/>
  <line x1=""178"" y1=""92"" x2=""178"" y2=""119"" stroke=""#111"" stroke-width=""3"" opacity="".8""/>

  <line x1=""236"" y1=""121"" x2=""236"" y2=""153"" stroke=""#111"" stroke-width=""3"" opacity="".6""/>
  <line x1=""240"" y1=""129"" x2=""363"" y2=""129"" stroke=""#ffffff"" stroke-width=""2"" opacity="".3""/>

  <circle cx=""116"" cy=""154"" r=""33"" fill=""#050505"" stroke=""#111"" stroke-width=""7""/>
  <circle cx=""116"" cy=""154"" r=""17"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>
  <circle cx=""316"" cy=""154"" r=""33"" fill=""#050505"" stroke=""#111"" stroke-width=""7""/>
  <circle cx=""316"" cy=""154"" r=""17"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>

  <text x=""210"" y=""200"" text-anchor=""middle"" fill=""#ffffff"" font-size=""12"" font-family=""Arial"" opacity="".85"">{make} {model}</text>
</svg>";
        }

        private static string BuildSuvSvg(VehicleRenderProfile p, string paint, string label, string make, string model)
        {
            return $@"
<svg class=""garage-render-svg"" viewBox=""0 0 420 210"" role=""img"" aria-label=""{label}"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <linearGradient id=""bodyGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#ffffff"" stop-opacity="".34""/>
      <stop offset=""35%"" stop-color=""{paint}""/>
      <stop offset=""100%"" stop-color=""#050505"" stop-opacity="".48""/>
    </linearGradient>
    <linearGradient id=""glassGrad"" x1=""0"" x2=""0"" y1=""0"" y2=""1"">
      <stop offset=""0%"" stop-color=""#d8f4ff""/>
      <stop offset=""100%"" stop-color=""#172632""/>
    </linearGradient>
  </defs>

  <ellipse cx=""210"" cy=""178"" rx=""176"" ry=""18"" fill=""#000"" opacity="".58""/>

  <path d=""M50 124 C64 96, 96 84, 142 84 L265 84 C306 88, 346 104, 371 130 L381 155 L45 155 Z""
        fill=""url(#bodyGrad)"" stroke=""#f2f2f2"" stroke-width=""3""/>

  <path d=""M112 91 L260 91 C292 95, 319 108, 340 127 L101 127 Z"" fill=""url(#glassGrad)"" opacity="".9""/>
  <line x1=""164"" y1=""92"" x2=""164"" y2=""127"" stroke=""#111"" stroke-width=""3"" opacity="".8""/>
  <line x1=""225"" y1=""92"" x2=""225"" y2=""127"" stroke=""#111"" stroke-width=""3"" opacity="".8""/>

  <circle cx=""116"" cy=""155"" r=""33"" fill=""#050505"" stroke=""#111"" stroke-width=""7""/>
  <circle cx=""116"" cy=""155"" r=""17"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>
  <circle cx=""306"" cy=""155"" r=""33"" fill=""#050505"" stroke=""#111"" stroke-width=""7""/>
  <circle cx=""306"" cy=""155"" r=""17"" fill=""#c6c9cc"" stroke=""#333"" stroke-width=""4""/>

  <text x=""210"" y=""200"" text-anchor=""middle"" fill=""#ffffff"" font-size=""12"" font-family=""Arial"" opacity="".85"">{make} {model}</text>
</svg>";
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }
    }
}